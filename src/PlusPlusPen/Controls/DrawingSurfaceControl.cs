using System.Diagnostics;
using System.Linq;
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
    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(
            nameof(Session),
            typeof(DrawingSessionService),
            typeof(DrawingSurfaceControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSessionChanged));

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Queue<(Point point, long ticks)> _smoothingWindow = new();
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
    private bool _cacheDirty = true;

    public DrawingSessionService? Session
    {
        get => (DrawingSessionService?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
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

        EnsureCommittedStrokeCache();
        if (_committedStrokeCache is not null)
        {
            drawingContext.DrawImage(_committedStrokeCache, new Rect(new Point(0, 0), RenderSize));
        }

        if (_activeStroke is not null)
        {
            StrokeRenderHelper.DrawStroke(drawingContext, _activeStroke);
        }

        if (Session.ActiveTool == ToolKind.Eraser && _hoverPoint is not null)
        {
            var radius = Math.Max(8, Session.Settings.EraserSize);
            var previewPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 60, 189, 91)), 1.4)
            {
                DashStyle = DashStyles.Solid
            };
            drawingContext.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(32, 60, 189, 91)),
                previewPen,
                _hoverPoint.Value,
                radius,
                radius);
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (Session is null || e.LeftButton != MouseButtonState.Pressed)
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
        if (Session is null)
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
        if (!_isDrawing)
        {
            return;
        }

        EndInteraction();
        e.Handled = true;
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
            EraseAt(rawPoint);
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
            AddPointToActiveStroke(rawPoint, pressure ?? 0.5f, isFirst: false);
            InvalidateVisual();
        }
        else
        {
            EraseAt(rawPoint);
        }
    }

    private void EndInteraction()
    {
        if (Session is not null && Session.ActiveTool == ToolKind.Pen && _activeStroke is { Points.Count: > 0 })
        {
            Session.PushHistorySnapshot();
            Session.AddStroke(_activeStroke);
        }

        if (Session is not null && Session.ActiveTool == ToolKind.Eraser && _eraserSnapshotTaken)
        {
            Session.RaiseStateSignals();
        }

        _activeStroke = null;
        _isDrawing = false;
        _smoothingWindow.Clear();

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

    private void AddPointToActiveStroke(Point rawPoint, float pressure, bool isFirst)
    {
        if (Session is null || _activeStroke is null)
        {
            return;
        }

        var now = _stopwatch.ElapsedMilliseconds;
        _smoothingWindow.Enqueue((rawPoint, now));
        var smoothingCount = Session.Settings.SmoothingEnabled
            ? Math.Clamp((int)Math.Round(2 + Session.Settings.SmoothingLevel * 6), 2, 8)
            : 1;
        while (_smoothingWindow.Count > smoothingCount)
        {
            _smoothingWindow.Dequeue();
        }

        var targetPoint = Session.Settings.SmoothingEnabled ? AveragePoint(_smoothingWindow) : rawPoint;
        if (!isFirst && ShouldSkipSample(targetPoint, now))
        {
            return;
        }

        var width = CalculateWidth(targetPoint, now, pressure, isFirst);
        AppendInterpolatedPoints(targetPoint, width, isFirst);
        _lastPoint = targetPoint;
        _lastTicks = now;
        _lastAcceptedTicks = now;
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

            return Math.Clamp(width, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
        }

        if (isFirst)
        {
            return Math.Clamp(baseWidth, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
        }

        if (!Session.Settings.MouseSpeedAffectsThickness)
        {
            return Math.Clamp(baseWidth, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
        }

        var distance = (point - _lastPoint).Length;
        var delta = Math.Max(1, ticks - _lastTicks);
        var speed = distance / delta;
        var sensitivity = Session.Settings.PenSensitivity switch
        {
            PenSensitivity.Low => 0.08,
            PenSensitivity.High => 0.18,
            _ => 0.12
        };
        var speedFactor = Math.Clamp(1.6 - speed * sensitivity, 0.65, 1.55);
        var result = baseWidth * speedFactor;
        if (Session.Settings.FountainPenEffectEnabled)
        {
            result *= 1.12;
        }

        return Math.Clamp(result, Session.Settings.MinimumStrokeThickness, Session.Settings.MaximumStrokeThickness);
    }

    private void EraseAt(Point point)
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

        var radius = Math.Max(8, Session.Settings.EraserSize);
        var replacement = new List<StrokeModel>();

        foreach (var stroke in Session.Strokes)
        {
            var fragments = Session.Settings.EraserMode == EraserMode.WholeStroke
                ? RemoveWholeStroke(stroke, point, radius)
                : SplitStroke(stroke, point, radius);
            replacement.AddRange(fragments);
        }

        Session.ReplaceStrokes(replacement);
    }

    private static IEnumerable<StrokeModel> RemoveWholeStroke(StrokeModel stroke, Point eraserCenter, double radius)
    {
        var intersects = stroke.Points.Any(point => (point.Position - eraserCenter).Length <= radius + point.Width * 0.5);
        return intersects ? [] : [stroke.Clone()];
    }

    private static IEnumerable<StrokeModel> SplitStroke(StrokeModel stroke, Point eraserCenter, double radius)
    {
        var segments = new List<StrokeModel>();
        StrokeModel? current = null;

        foreach (var point in stroke.Points)
        {
            var isRemoved = (point.Position - eraserCenter).Length <= radius + point.Width * 0.5;
            if (isRemoved)
            {
                if (current is { Points.Count: > 1 })
                {
                    segments.Add(current);
                }

                current = null;
                continue;
            }

            current ??= new StrokeModel(stroke.Color);
            current.Points.Add(new StrokePointModel(point.Position, point.Width));
        }

        if (current is { Points.Count: > 1 })
        {
            segments.Add(current);
        }

        return segments;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverPoint = null;
        InvalidateVisual();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _cacheDirty = true;
        InvalidateVisual();
    }

    private void AppendInterpolatedPoints(Point targetPoint, double targetWidth, bool isFirst)
    {
        if (_activeStroke is null)
        {
            return;
        }

        if (isFirst || _activeStroke.Points.Count == 0)
        {
            _activeStroke.Points.Add(new StrokePointModel(targetPoint, targetWidth));
            return;
        }

        var previous = _activeStroke.Points[^1];
        var distance = (targetPoint - previous.Position).Length;
        var spacing = Math.Max(1.2, Math.Min(previous.Width, targetWidth) * 0.45);
        var steps = Math.Clamp((int)Math.Ceiling(distance / spacing), 1, 12);

        for (var index = 1; index <= steps; index++)
        {
            var t = index / (double)steps;
            var interpolatedPoint = new Point(
                Lerp(previous.Position.X, targetPoint.X, t),
                Lerp(previous.Position.Y, targetPoint.Y, t));
            var interpolatedWidth = Lerp(previous.Width, targetWidth, t);
            _activeStroke.Points.Add(new StrokePointModel(interpolatedPoint, interpolatedWidth));
        }
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + (end - start) * amount;
    }

    private bool ShouldSkipSample(Point targetPoint, long now)
    {
        var distance = (targetPoint - _lastPoint).Length;
        var delta = now - _lastAcceptedTicks;
        return delta < 2 && distance < 0.9;
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

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(RenderSize.Width * dpi.DpiScaleX));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(RenderSize.Height * dpi.DpiScaleY));
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            foreach (var stroke in Session.Strokes)
            {
                StrokeRenderHelper.DrawStroke(context, stroke);
            }
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        _committedStrokeCache = bitmap;
        _cacheRenderSize = RenderSize;
        _cacheDpi = dpi;
        _cacheDirty = false;
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
}
