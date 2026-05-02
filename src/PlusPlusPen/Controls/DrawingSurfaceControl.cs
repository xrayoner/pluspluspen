using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PlusPlusPen.Helpers;
using PlusPlusPen.Models;
using PlusPlusPen.Services;

namespace PlusPlusPen.Controls;

public sealed class DrawingSurfaceControl : FrameworkElement
{
    private const int EraserCommitIntervalMs = 33;
    private const int CacheRebuildThrottleMs = 33;
    private const double MinimumEraserFragmentLength = 1.5;

    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(
            nameof(Session),
            typeof(DrawingSessionService),
            typeof(DrawingSurfaceControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSessionChanged));

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Queue<(Point point, long ticks)> _smoothingWindow = new();
    private readonly List<EraserStrokeHitEntry> _eraserHitEntries = [];
    private RenderTargetBitmap? _committedStrokeCache;
    private StrokeModel? _activeStroke;
    private Size _cacheRenderSize;
    private DpiScale _cacheDpi;
    private Point _lastPoint;
    private long _lastTicks;
    private long _lastAcceptedTicks;
    private bool _isDrawing;
    private bool _eraserSnapshotTaken;
    private Point? _hoverPoint;
    private int? _activeTouchId;
    private bool _cacheDirty = true;
    private bool _cacheRefreshScheduled;
    private long _lastCacheRebuildTicks;
    private long _lastEraserCommitTicks;
    private bool _eraserDirtySinceCommit;
    private Point? _lastEraserPoint;

    public DrawingSessionService? Session
    {
        get => (DrawingSessionService?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public DrawingSurfaceControl()
    {
        IsManipulationEnabled = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

        if (Session is null)
        {
            return;
        }

        if (Session.BackgroundSnapshot is not null)
        {
            drawingContext.DrawImage(Session.BackgroundSnapshot, new Rect(new Point(0, 0), RenderSize));
        }

        if (IsRenderingLiveEraserState())
        {
            DrawLiveEraserState(drawingContext);
        }
        else
        {
            EnsureCommittedStrokeCache();
            if (_committedStrokeCache is not null)
            {
                drawingContext.DrawImage(_committedStrokeCache, new Rect(new Point(0, 0), RenderSize));
            }
        }

        if (_activeStroke is not null)
        {
            StrokeRenderHelper.DrawStroke(
                drawingContext,
                _activeStroke,
                options: StrokeRenderHelper.CreateRenderOptions(Session.Settings, isLivePreview: true));
        }

        if (Session.ActiveTool == ToolKind.Eraser && _hoverPoint is not null)
        {
            var radius = GetEraserRadius();
            var previewPen = new Pen(new SolidColorBrush(Color.FromArgb(240, 52, 176, 255)), 2.0)
            {
                DashStyle = DashStyles.Dash
            };
            drawingContext.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(42, 92, 206, 255)),
                previewPen,
                _hoverPoint.Value,
                radius,
                radius);
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (_activeTouchId is not null || Session is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Focus();
        CaptureMouse();
        StartInteraction(e.GetPosition(this), 0.5f);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_activeTouchId is not null)
        {
            return;
        }

        if (!_isDrawing || Session is null || e.LeftButton != MouseButtonState.Pressed)
        {
            if (Session?.ActiveTool == ToolKind.Eraser)
            {
                _hoverPoint = e.GetPosition(this);
                InvalidateVisual();
            }

            return;
        }

        ContinueInteraction(e.GetPosition(this), null);
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_activeTouchId is not null)
        {
            return;
        }

        if (!_isDrawing)
        {
            return;
        }

        EndInteraction();
        e.Handled = true;
    }

    protected override void OnStylusDown(StylusDownEventArgs e)
    {
        base.OnStylusDown(e);
        if (_activeTouchId is not null || Session is null)
        {
            return;
        }

        CaptureStylus();
        Focus();
        var points = e.GetStylusPoints(this);
        var pressure = points.Count > 0 ? points[0].PressureFactor : 0.5f;
        StartInteraction(e.GetPosition(this), pressure);
        e.Handled = true;
    }

    protected override void OnStylusMove(StylusEventArgs e)
    {
        base.OnStylusMove(e);
        if (_activeTouchId is not null)
        {
            return;
        }

        if (!_isDrawing || Session is null)
        {
            if (Session?.ActiveTool == ToolKind.Eraser)
            {
                _hoverPoint = e.GetPosition(this);
                InvalidateVisual();
            }

            return;
        }

        var points = e.GetStylusPoints(this);
        var pressure = points.Count > 0 ? points[^1].PressureFactor : (float?)null;
        ContinueInteraction(e.GetPosition(this), pressure);
        e.Handled = true;
    }

    protected override void OnStylusUp(StylusEventArgs e)
    {
        base.OnStylusUp(e);
        if (_activeTouchId is not null)
        {
            return;
        }

        if (!_isDrawing)
        {
            return;
        }

        EndInteraction();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverPoint = null;
        InvalidateVisual();
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        if (Session is null)
        {
            return;
        }

        if (_activeTouchId is not null && _activeTouchId != e.TouchDevice.Id)
        {
            e.Handled = true;
            return;
        }

        _activeTouchId = e.TouchDevice.Id;
        CaptureTouch(e.TouchDevice);
        Focus();
        StartInteraction(e.GetTouchPoint(this).Position, 0.5f);
        e.Handled = true;
    }

    protected override void OnTouchMove(TouchEventArgs e)
    {
        base.OnTouchMove(e);
        if (_activeTouchId != e.TouchDevice.Id)
        {
            e.Handled = true;
            return;
        }

        if (!_isDrawing || Session is null)
        {
            return;
        }

        ContinueInteraction(e.GetTouchPoint(this).Position, null);
        e.Handled = true;
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        if (_activeTouchId != e.TouchDevice.Id)
        {
            e.Handled = true;
            return;
        }

        EndInteraction();
        ReleaseTouchCapture(e.TouchDevice);
        _activeTouchId = null;
        e.Handled = true;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _cacheDirty = true;
        InvalidateVisual();
    }

    private static void OnSessionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (DrawingSurfaceControl)d;
        if (e.OldValue is DrawingSessionService oldSession)
        {
            oldSession.VisualStateChanged -= control.HandleVisualStateChanged;
        }

        if (e.NewValue is DrawingSessionService newSession)
        {
            newSession.VisualStateChanged += control.HandleVisualStateChanged;
            control._cacheDirty = true;
        }
    }

    private void HandleVisualStateChanged(object? sender, EventArgs e)
    {
        _cacheDirty = true;
        InvalidateVisual();
        ScheduleDeferredCacheRefresh();
    }

    private void StartInteraction(Point rawPoint, float pressure)
    {
        if (Session is null)
        {
            return;
        }

        _isDrawing = true;
        _smoothingWindow.Clear();
        _lastTicks = _stopwatch.ElapsedMilliseconds;
        _lastAcceptedTicks = _lastTicks;
        _lastPoint = rawPoint;
        _eraserSnapshotTaken = false;

        if (Session.ActiveTool == ToolKind.Pen)
        {
            _activeStroke = new StrokeModel(Session.SelectedColor);
            AddPointToActiveStroke(rawPoint, pressure, isFirst: true);
        }
        else
        {
            BeginEraserStroke(rawPoint);
        }
    }

    private void ContinueInteraction(Point rawPoint, float? pressure)
    {
        if (Session is null)
        {
            return;
        }

        if (Session.ActiveTool == ToolKind.Pen)
        {
            var accepted = AddPointToActiveStroke(rawPoint, pressure ?? 0.5f, isFirst: false);
            if (accepted)
            {
                InvalidateVisual();
            }
        }
        else
        {
            var stopwatch = Stopwatch.StartNew();
            ContinueEraserStroke(rawPoint);
            stopwatch.Stop();
            LogDebug($"Eraser move processing: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
            InvalidateVisual();
        }
    }

    private void EndInteraction()
    {
        if (Session is not null && Session.ActiveTool == ToolKind.Pen && _activeStroke is { Points.Count: > 0 })
        {
            _activeStroke = FinalizeCompletedStroke(_activeStroke, Session.Settings);

            Session.PushHistorySnapshot();
            Session.AddStroke(_activeStroke);
        }

        if (Session is not null && Session.ActiveTool == ToolKind.Eraser && _eraserSnapshotTaken)
        {
            FlushEraserChanges(force: true);
            Session.RaiseStateSignals();
            _cacheDirty = true;
            InvalidateVisual();
        }

        _activeStroke = null;
        _isDrawing = false;
        _activeTouchId = null;
        _smoothingWindow.Clear();
        ClearEraserState();

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        if (IsStylusCaptured)
        {
            ReleaseStylusCapture();
        }

        InvalidateVisual();
    }

    private bool AddPointToActiveStroke(Point rawPoint, float pressure, bool isFirst)
    {
        if (Session is null || _activeStroke is null)
        {
            return false;
        }

        var now = _stopwatch.ElapsedMilliseconds;
        _smoothingWindow.Enqueue((rawPoint, now));
        var smoothingCount = GetLiveSmoothingSampleCount(Session.Settings);
        while (_smoothingWindow.Count > smoothingCount)
        {
            _smoothingWindow.Dequeue();
        }

        var targetPoint = smoothingCount > 1 ? AveragePoint(_smoothingWindow) : rawPoint;
        if (!isFirst && ShouldSkipSample(targetPoint, now))
        {
            return false;
        }

        var width = CalculateWidth(targetPoint, now, pressure, isFirst);
        AppendInterpolatedPoints(targetPoint, width, isFirst);
        _lastPoint = targetPoint;
        _lastTicks = now;
        _lastAcceptedTicks = now;
        return true;
    }

    private double CalculateWidth(Point point, long ticks, float pressure, bool isFirst)
    {
        if (Session is null)
        {
            return 4;
        }

        var baseWidth = Session.SelectedThickness;
        if (!Session.Settings.DynamicThicknessEnabled)
        {
            return Math.Clamp(baseWidth, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
        }

        var styleBaseMultiplier = Session.Settings.PenStyle switch
        {
            PenStyleOption.Soft => 1.04,
            PenStyleOption.FountainPen => 1.14,
            PenStyleOption.FeltTip => 1.2,
            _ => 1.0
        };
        baseWidth *= styleBaseMultiplier;

        if (Session.Settings.StylusPressureEnabled && Stylus.CurrentStylusDevice is not null)
        {
            var penFactor = Session.Settings.PenSensitivity switch
            {
                PenSensitivity.Low => 1.0,
                PenSensitivity.High => 1.8,
                _ => 1.35
            };
            var width = baseWidth * (0.55 + pressure * penFactor);
            if (Session.Settings.FountainPenEffectEnabled)
            {
                width *= 1.08;
            }

            if (Session.Settings.VelocityBasedThicknessEnabled && !isFirst)
            {
                width *= CalculateVelocityFactor(point, ticks, 0.06);
            }

            return Math.Clamp(width, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
        }

        if (isFirst || !Session.Settings.MouseSpeedAffectsThickness || !Session.Settings.VelocityBasedThicknessEnabled)
        {
            return Math.Clamp(baseWidth, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
        }

        var speedFactor = CalculateVelocityFactor(point, ticks, Session.Settings.PenSensitivity switch
        {
            PenSensitivity.Low => 0.08,
            PenSensitivity.High => 0.18,
            _ => 0.12
        });
        var result = baseWidth * speedFactor;
        if (Session.Settings.FountainPenEffectEnabled)
        {
            result *= 1.12;
        }

        return Math.Clamp(result, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
    }

    private void BeginEraserStroke(Point point)
    {
        if (Session is null)
        {
            return;
        }

        _hoverPoint = point;

        if (!_eraserSnapshotTaken)
        {
            Session.PushHistorySnapshot();
            _eraserSnapshotTaken = true;
        }

        BuildEraserHitEntries();
        _lastEraserCommitTicks = _stopwatch.ElapsedMilliseconds;
        _eraserDirtySinceCommit = false;
        _lastEraserPoint = null;
        SampleEraserPath(point);
        FlushEraserChanges(force: true);
    }

    private void ContinueEraserStroke(Point point)
    {
        _hoverPoint = point;
        SampleEraserPath(point);

        var now = _stopwatch.ElapsedMilliseconds;
        if (now - _lastEraserCommitTicks >= EraserCommitIntervalMs)
        {
            FlushEraserChanges(force: false);
        }
    }

    private void BuildEraserHitEntries()
    {
        _eraserHitEntries.Clear();

        if (Session is null)
        {
            return;
        }

        for (var strokeIndex = 0; strokeIndex < Session.Strokes.Count; strokeIndex++)
        {
            var stroke = Session.Strokes[strokeIndex];
            if (stroke.Points.Count == 0)
            {
                continue;
            }

            stroke.RecalculateBounds();
            var segments = new List<EraserSegment>(Math.Max(1, stroke.Points.Count - 1));
            for (var pointIndex = 1; pointIndex < stroke.Points.Count; pointIndex++)
            {
                var previous = stroke.Points[pointIndex - 1];
                var point = stroke.Points[pointIndex];
                var radius = Math.Max(previous.Width, point.Width) * 0.5;
                segments.Add(new EraserSegment(previous.Position, point.Position, radius));
            }

            _eraserHitEntries.Add(new EraserStrokeHitEntry(stroke, stroke.Bounds, segments));
        }
    }

    private void SampleEraserPath(Point point)
    {
        var eraserRadius = GetEraserRadius();
        if (_lastEraserPoint is null)
        {
            TestEraserPoint(point, eraserRadius);
            _lastEraserPoint = point;
            return;
        }

        var start = _lastEraserPoint.Value;
        var delta = point - start;
        var distance = delta.Length;
        if (distance <= 0.01)
        {
            TestEraserPoint(point, eraserRadius);
            _lastEraserPoint = point;
            return;
        }

        var spacing = Math.Max(1.0, eraserRadius * 0.5);
        var steps = Math.Clamp((int)Math.Ceiling(distance / spacing), 1, 24);
        for (var step = 1; step <= steps; step++)
        {
            var t = step / (double)steps;
            var sample = new Point(
                Lerp(start.X, point.X, t),
                Lerp(start.Y, point.Y, t));
            TestEraserPoint(sample, eraserRadius);
        }

        _lastEraserPoint = point;
    }

    private void TestEraserPoint(Point eraserCenter, double eraserRadius)
    {
        var searchBounds = new Rect(
            eraserCenter.X - eraserRadius,
            eraserCenter.Y - eraserRadius,
            eraserRadius * 2,
            eraserRadius * 2);

        for (var index = 0; index < _eraserHitEntries.Count; index++)
        {
            var entry = _eraserHitEntries[index];
            if (entry.Fragments.Count == 0 || !entry.Bounds.IntersectsWith(searchBounds))
            {
                continue;
            }

            if (ApplyPartialErase(entry, eraserCenter, eraserRadius))
            {
                entry.RebuildBounds();
                _eraserDirtySinceCommit = true;
            }
        }
    }

    private static bool ApplyPartialErase(EraserStrokeHitEntry entry, Point eraserCenter, double eraserRadius)
    {
        if (entry.Fragments.Count == 0)
        {
            return false;
        }

        var changed = false;
        var updatedFragments = new List<StrokeModel>(entry.Fragments.Count);
        foreach (var fragment in entry.Fragments)
        {
            if (!fragment.Bounds.IntersectsWith(new Rect(
                    eraserCenter.X - eraserRadius,
                    eraserCenter.Y - eraserRadius,
                    eraserRadius * 2,
                    eraserRadius * 2)))
            {
                updatedFragments.Add(fragment);
                continue;
            }

            var split = SplitFragmentByEraser(fragment, eraserCenter, eraserRadius);
            if (split.FragmentChanged)
            {
                changed = true;
            }

            updatedFragments.AddRange(split.Fragments);
        }

        if (!changed)
        {
            return false;
        }

        entry.Fragments.Clear();
        entry.Fragments.AddRange(updatedFragments);
        return true;
    }

    private void FlushEraserChanges(bool force)
    {
        if (Session is null || (!force && !_eraserDirtySinceCommit))
        {
            return;
        }

        var replacement = new List<StrokeModel>();
        foreach (var entry in _eraserHitEntries)
        {
            if (entry.Fragments.Count == 0)
            {
                continue;
            }

            replacement.AddRange(entry.Fragments);
        }

        Session.ReplaceStrokes(replacement);
        _lastEraserCommitTicks = _stopwatch.ElapsedMilliseconds;
        _eraserDirtySinceCommit = false;
        _cacheDirty = true;
        _committedStrokeCache = null;

        if (force)
        {
            _lastCacheRebuildTicks = 0;
        }
        else
        {
            ScheduleDeferredCacheRefresh();
        }
    }

    private void ClearEraserState()
    {
        _eraserHitEntries.Clear();
        _eraserDirtySinceCommit = false;
        _lastEraserPoint = null;
    }

    private double GetEraserRadius()
    {
        return Session is null ? 8 : Math.Max(1.0, Session.Settings.EraserSize);
    }

    private static double DistancePointToSegment(Point point, Point start, Point end)
    {
        var segment = end - start;
        var segmentLengthSquared = segment.X * segment.X + segment.Y * segment.Y;
        if (segmentLengthSquared <= 0.0001)
        {
            return (point - start).Length;
        }

        var pointVector = point - start;
        var projection = (pointVector.X * segment.X + pointVector.Y * segment.Y) / segmentLengthSquared;
        var t = Math.Clamp(projection, 0, 1);
        var nearest = new Point(start.X + segment.X * t, start.Y + segment.Y * t);
        return (point - nearest).Length;
    }

    private static EraserSplitResult SplitFragmentByEraser(StrokeModel fragment, Point eraserCenter, double eraserRadius)
    {
        if (fragment.Points.Count == 0)
        {
            return EraserSplitResult.UnchangedEmpty;
        }

        if (fragment.Points.Count == 1)
        {
            var onlyPoint = fragment.Points[0];
            var hit = (onlyPoint.Position - eraserCenter).Length <= eraserRadius + onlyPoint.Width * 0.5;
            return hit
                ? new EraserSplitResult([], true)
                : new EraserSplitResult([fragment], false);
        }

        var output = new List<StrokeModel>();
        StrokeModel? current = null;
        var changed = false;

        for (var index = 1; index < fragment.Points.Count; index++)
        {
            var previous = fragment.Points[index - 1];
            var next = fragment.Points[index];
            var segmentRadius = Math.Max(previous.Width, next.Width) * 0.5;
            var segmentHit = DistancePointToSegment(eraserCenter, previous.Position, next.Position) <= eraserRadius + segmentRadius;

            if (segmentHit)
            {
                changed = true;
                if (current is not null)
                {
                    AddFragmentIfValid(output, current);
                    current = null;
                }

                continue;
            }

            current ??= new StrokeModel(fragment.Color);
            if (current.Points.Count == 0)
            {
                current.AppendPoint(new StrokePointModel(previous.Position, previous.Width));
            }

            current.AppendPoint(new StrokePointModel(next.Position, next.Width));
        }

        if (current is not null)
        {
            AddFragmentIfValid(output, current);
        }

        return new EraserSplitResult(output, changed);
    }

    private static void AddFragmentIfValid(List<StrokeModel> output, StrokeModel candidate)
    {
        if (candidate.Points.Count < 2)
        {
            return;
        }

        var totalLength = 0d;
        for (var index = 1; index < candidate.Points.Count; index++)
        {
            totalLength += (candidate.Points[index].Position - candidate.Points[index - 1].Position).Length;
        }

        if (totalLength < MinimumEraserFragmentLength)
        {
            return;
        }

        candidate.RecalculateBounds();
        output.Add(candidate);
    }

    private static Rect CreateBoundsForStrokes(IEnumerable<StrokeModel> strokes)
    {
        var bounds = Rect.Empty;
        foreach (var stroke in strokes)
        {
            bounds = bounds.IsEmpty ? stroke.Bounds : Rect.Union(bounds, stroke.Bounds);
        }

        return bounds;
    }

    private void AppendInterpolatedPoints(Point targetPoint, double targetWidth, bool isFirst)
    {
        if (_activeStroke is null)
        {
            return;
        }

        if (isFirst || _activeStroke.Points.Count == 0)
        {
            _activeStroke.AppendPoint(new StrokePointModel(targetPoint, targetWidth));
            return;
        }

        var previous = _activeStroke.Points[^1];
        var distance = (targetPoint - previous.Position).Length;
        var spacing = Math.Max(1.2, Math.Min(previous.Width, targetWidth) * 0.45);
        var limit = Session?.Settings.InterpolationLimit ?? 8;
        var steps = Math.Clamp((int)Math.Ceiling(distance / spacing), 1, limit);

        for (var index = 1; index <= steps; index++)
        {
            var t = index / (double)steps;
            var interpolatedPoint = new Point(
                Lerp(previous.Position.X, targetPoint.X, t),
                Lerp(previous.Position.Y, targetPoint.Y, t));
            var interpolatedWidth = Lerp(previous.Width, targetWidth, t);
            _activeStroke.AppendPoint(new StrokePointModel(interpolatedPoint, interpolatedWidth));
        }
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + (end - start) * amount;
    }

    private bool IsRenderingLiveEraserState()
    {
        return Session is not null
            && _isDrawing
            && Session.ActiveTool == ToolKind.Eraser
            && _eraserHitEntries.Count > 0;
    }

    private void DrawLiveEraserState(DrawingContext drawingContext)
    {
        if (Session is null)
        {
            return;
        }

        var options = StrokeRenderHelper.CreateRenderOptions(Session.Settings, isLivePreview: false);
        foreach (var entry in _eraserHitEntries)
        {
            foreach (var fragment in entry.Fragments)
            {
                StrokeRenderHelper.DrawStroke(drawingContext, fragment, options: options);
            }
        }
    }

    private static StrokeModel FinalizeCompletedStroke(StrokeModel stroke, AppSettingsModel settings)
    {
        if (stroke.Points.Count < 3)
        {
            return stroke.Clone();
        }

        if (settings.SmoothingPreset == SmoothingPresetOption.Off && !settings.StrokeTaperEnabled)
        {
            return stroke.Clone();
        }

        var passCount = settings.SmoothingPreset switch
        {
            SmoothingPresetOption.Low => 1,
            SmoothingPresetOption.High => 3,
            SmoothingPresetOption.Medium => 2,
            _ => 0
        };

        if (settings.PenStyle is PenStyleOption.Soft or PenStyleOption.FountainPen)
        {
            passCount++;
        }

        var sourcePoints = stroke.Points
            .Select(point => new StrokePointModel(point.Position, point.Width))
            .ToList();
        var smoothingFactor = settings.SmoothingPreset switch
        {
            SmoothingPresetOption.Low => 0.18,
            SmoothingPresetOption.High => 0.34,
            SmoothingPresetOption.Medium => 0.26,
            _ => 0.0
        };

        for (var pass = 0; pass < passCount; pass++)
        {
            sourcePoints = SmoothPoints(sourcePoints, smoothingFactor);
        }

        var smoothed = new StrokeModel(stroke.Color);
        foreach (var point in sourcePoints)
        {
            smoothed.AppendPoint(point);
        }

        return smoothed;
    }

    private bool ShouldSkipSample(Point targetPoint, long now)
    {
        if (Session is null)
        {
            return true;
        }

        var minDistance = Math.Max(0.0, Session.Settings.MinimumPointDistance);
        var distance = (targetPoint - _lastPoint).Length;
        if (distance < minDistance)
        {
            return true;
        }

        var delta = now - _lastAcceptedTicks;
        return delta < 3 && distance < Math.Max(0.75, minDistance * 0.25);
    }

    private void EnsureCommittedStrokeCache()
    {
        if (Session is null || RenderSize.Width <= 0 || RenderSize.Height <= 0)
        {
            _committedStrokeCache = null;
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        if (!_cacheDirty
            && _committedStrokeCache is not null
            && _cacheRenderSize == RenderSize
            && Math.Abs(_cacheDpi.DpiScaleX - dpi.DpiScaleX) < 0.001
            && Math.Abs(_cacheDpi.DpiScaleY - dpi.DpiScaleY) < 0.001)
        {
            return;
        }

        var now = _stopwatch.ElapsedMilliseconds;
        if (_committedStrokeCache is not null && now - _lastCacheRebuildTicks < CacheRebuildThrottleMs)
        {
            ScheduleDeferredCacheRefresh();
            return;
        }

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(RenderSize.Width * dpi.DpiScaleX));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(RenderSize.Height * dpi.DpiScaleY));
        var rebuildWatch = Stopwatch.StartNew();

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            foreach (var stroke in Session.Strokes)
            {
                StrokeRenderHelper.DrawStroke(
                    context,
                    stroke,
                    options: StrokeRenderHelper.CreateRenderOptions(Session.Settings, isLivePreview: false));
            }
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        _committedStrokeCache = bitmap;
        _cacheRenderSize = RenderSize;
        _cacheDpi = dpi;
        _cacheDirty = false;
        _lastCacheRebuildTicks = now;
        rebuildWatch.Stop();
        LogDebug($"Cache rebuild: {rebuildWatch.Elapsed.TotalMilliseconds:F2} ms");
    }

    private void ScheduleDeferredCacheRefresh()
    {
        if (_cacheRefreshScheduled)
        {
            return;
        }

        _cacheRefreshScheduled = true;
        _ = Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(CacheRebuildThrottleMs);
                InvalidateVisual();
            }
            finally
            {
                _cacheRefreshScheduled = false;
            }
        });
    }

    private static Point AveragePoint(IEnumerable<(Point point, long ticks)> points)
    {
        double x = 0;
        double y = 0;
        var count = 0;

        foreach (var item in points)
        {
            x += item.point.X;
            y += item.point.Y;
            count++;
        }

        return new Point(x / count, y / count);
    }

    private static List<StrokePointModel> SmoothPoints(IReadOnlyList<StrokePointModel> points, double smoothingFactor)
    {
        if (points.Count < 3 || smoothingFactor <= 0)
        {
            return points.ToList();
        }

        var smoothed = new List<StrokePointModel>(points.Count)
        {
            new(points[0].Position, points[0].Width)
        };

        for (var index = 1; index < points.Count - 1; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            var next = points[index + 1];
            var x = previous.Position.X * smoothingFactor + current.Position.X * (1 - 2 * smoothingFactor) + next.Position.X * smoothingFactor;
            var y = previous.Position.Y * smoothingFactor + current.Position.Y * (1 - 2 * smoothingFactor) + next.Position.Y * smoothingFactor;
            var width = previous.Width * 0.2 + current.Width * 0.6 + next.Width * 0.2;
            smoothed.Add(new StrokePointModel(new Point(x, y), width));
        }

        smoothed.Add(new StrokePointModel(points[^1].Position, points[^1].Width));
        return smoothed;
    }

    private static int GetLiveSmoothingSampleCount(AppSettingsModel settings)
    {
        if (!settings.LiveSmoothingEnabled && settings.SmoothingPreset == SmoothingPresetOption.Off)
        {
            return 1;
        }

        return settings.SmoothingPreset switch
        {
            SmoothingPresetOption.Low => 2,
            SmoothingPresetOption.High => 5,
            SmoothingPresetOption.Medium => 4,
            _ => settings.LiveSmoothingEnabled ? 3 : 1
        };
    }

    private double CalculateVelocityFactor(Point point, long ticks, double sensitivity)
    {
        var distance = (point - _lastPoint).Length;
        var delta = Math.Max(1, ticks - _lastTicks);
        var speed = distance / delta;
        return Math.Clamp(1.62 - speed * sensitivity, 0.62, 1.58);
    }

    private readonly record struct EraserSegment(Point Start, Point End, double StrokeRadius);

    private readonly record struct EraserSplitResult(List<StrokeModel> Fragments, bool FragmentChanged)
    {
        public static EraserSplitResult UnchangedEmpty { get; } = new([], false);
    }

    private sealed class EraserStrokeHitEntry
    {
        public EraserStrokeHitEntry(StrokeModel stroke, Rect bounds, List<EraserSegment> segments)
        {
            Bounds = bounds;
            Segments = segments;
            Fragments = [stroke.Clone()];
        }

        public Rect Bounds { get; private set; }

        public List<EraserSegment> Segments { get; }

        public List<StrokeModel> Fragments { get; }

        public void RebuildBounds()
        {
            foreach (var fragment in Fragments)
            {
                fragment.RecalculateBounds();
            }

            Bounds = CreateBoundsForStrokes(Fragments);
        }
    }

    [Conditional("DEBUG")]
    private static void LogDebug(string message)
    {
        Debug.WriteLine($"[++PEN][Drawing] {message}");
    }
}
