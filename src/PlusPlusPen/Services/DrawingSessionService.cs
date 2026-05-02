using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class DrawingSessionService : INotifyPropertyChanged
{
    private readonly DrawingHistoryService _history = new();
    private AppSettingsModel _settings;
    private ToolKind _activeTool;
    private Color _selectedColor = Colors.DodgerBlue;
    private double _selectedThickness;
    private bool _overlayVisible;
    private bool _isDrawingSessionActive;
    private BitmapSource? _backgroundSnapshot;

    public DrawingSessionService(AppSettingsModel settings)
    {
        _settings = settings;
        _selectedThickness = settings.DefaultThickness;
        _activeTool = ToolKind.None;
        Palette = new ReadOnlyCollection<PaletteColorModel>(
        [
            new PaletteColorModel("Black", Colors.Black),
            new PaletteColorModel("Blue", Color.FromRgb(35, 130, 255)),
            new PaletteColorModel("Red", Color.FromRgb(232, 75, 91)),
            new PaletteColorModel("Green", Color.FromRgb(53, 189, 91)),
            new PaletteColorModel("Yellow", Color.FromRgb(255, 209, 74))
        ]);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? VisualStateChanged;

    public ReadOnlyCollection<PaletteColorModel> Palette { get; }

    public List<StrokeModel> Strokes { get; } = [];

    public ToolKind ActiveTool
    {
        get => _activeTool;
        set => SetField(ref _activeTool, value);
    }

    public Color SelectedColor
    {
        get => _selectedColor;
        set => SetField(ref _selectedColor, value);
    }

    public double SelectedThickness
    {
        get => _selectedThickness;
        set => SetField(ref _selectedThickness, value);
    }

    public bool OverlayVisible
    {
        get => _overlayVisible;
        set => SetField(ref _overlayVisible, value);
    }

    public bool IsDrawingSessionActive
    {
        get => _isDrawingSessionActive;
        private set => SetField(ref _isDrawingSessionActive, value);
    }

    public bool IsModeTintVisible => _overlayVisible
        && _isDrawingSessionActive
        && _backgroundSnapshot is not null
        && (_activeTool == ToolKind.Pen || _activeTool == ToolKind.Eraser);

    public AppSettingsModel Settings
    {
        get => _settings;
        private set => SetField(ref _settings, value);
    }

    public BitmapSource? BackgroundSnapshot
    {
        get => _backgroundSnapshot;
        private set => SetField(ref _backgroundSnapshot, value);
    }

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public void PushHistorySnapshot()
    {
        _history.PushSnapshot(Strokes);
        RaiseStateSignals();
    }

    public void AddStroke(StrokeModel stroke)
    {
        if (stroke.Bounds.IsEmpty)
        {
            stroke.RecalculateBounds();
        }

        Strokes.Add(stroke);
        RaiseStateSignals();
    }

    public void ReplaceStrokes(IEnumerable<StrokeModel> strokes)
    {
        Strokes.Clear();
        Strokes.AddRange(strokes.Select(x => x.Clone()));
        RaiseStateSignals();
    }

    public void RemoveStroke(StrokeModel stroke)
    {
        Strokes.Remove(stroke);
        RaiseStateSignals();
    }

    public void Undo()
    {
        var previous = _history.Undo(Strokes);
        if (previous is null)
        {
            return;
        }

        Strokes.Clear();
        Strokes.AddRange(previous);
        RaiseStateSignals();
    }

    public void Redo()
    {
        var next = _history.Redo(Strokes);
        if (next is null)
        {
            return;
        }

        Strokes.Clear();
        Strokes.AddRange(next);
        RaiseStateSignals();
    }

    public void ApplySettings(AppSettingsModel settings)
    {
        Settings = settings;
        SelectedThickness = settings.DefaultThickness;
        if (!IsDrawingSessionActive)
        {
            ActiveTool = ToolKind.None;
        }
        RaiseStateSignals();
    }

    public void InitializeOverlaySurface(BitmapSource snapshot)
    {
        IsDrawingSessionActive = true;
        BackgroundSnapshot = snapshot;
        Strokes.Clear();
        _history.Clear();
        RaiseStateSignals();
    }

    public void SetBackgroundSnapshot(BitmapSource snapshot)
    {
        IsDrawingSessionActive = true;
        BackgroundSnapshot = snapshot;
        RaiseStateSignals();
    }

    public void BeginOverlaySession()
    {
        IsDrawingSessionActive = true;
        RaiseStateSignals();
    }

    public void ResetOverlaySurface()
    {
        ActiveTool = ToolKind.None;
        IsDrawingSessionActive = false;
        BackgroundSnapshot = null;
        Strokes.Clear();
        _history.Clear();
        RaiseStateSignals();
    }

    public void ClearAll()
    {
        PushHistorySnapshot();
        Strokes.Clear();
        RaiseStateSignals();
    }

    public void RaiseStateSignals()
    {
        RaisePropertyChanged(nameof(CanUndo));
        RaisePropertyChanged(nameof(CanRedo));
        VisualStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        if (propertyName is nameof(ActiveTool)
            or nameof(OverlayVisible)
            or nameof(IsDrawingSessionActive)
            or nameof(BackgroundSnapshot))
        {
            RaisePropertyChanged(nameof(IsModeTintVisible));
        }
        return true;
    }

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
