using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PlusPlusPen.Models;
using PlusPlusPen.Services;
using PlusPlusPen.Views;

namespace PlusPlusPen.ViewModels;

public sealed class ToolbarViewModel : ViewModelBase
{
    private readonly ServiceContainer _services;
    private readonly DrawingSessionService _session;
    private readonly ObservableCollection<double> _thicknessOptions = [4, 7, 11, 16];

    public ToolbarViewModel(ServiceContainer services)
    {
        _services = services;
        _session = services.DrawingSessionService;

        SelectPenCommand = new RelayCommand(SelectPen);
        SelectEraserCommand = new RelayCommand(SelectEraser);
        UndoCommand = new RelayCommand(Undo, () => _session.CanUndo);
        RedoCommand = new RelayCommand(Redo, () => _session.CanRedo);
        SaveCommand = new RelayCommand(SaveDrawing);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        SelectColorCommand = new RelayCommand(SelectColor);
        SelectThicknessCommand = new RelayCommand(SelectThickness);

        _session.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(DrawingSessionService.ActiveTool)
                or nameof(DrawingSessionService.SelectedColor)
                or nameof(DrawingSessionService.SelectedThickness)
                or nameof(DrawingSessionService.OverlayVisible))
            {
                RaiseToolbarState();
            }

            if (args.PropertyName is nameof(DrawingSessionService.CanUndo) or nameof(DrawingSessionService.CanRedo))
            {
                ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
            }
        };
    }

    public ICommand SelectPenCommand { get; }

    public ICommand SelectEraserCommand { get; }

    public ICommand UndoCommand { get; }

    public ICommand RedoCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand SelectColorCommand { get; }

    public ICommand SelectThicknessCommand { get; }

    public IEnumerable<PaletteColorModel> Palette => _session.Palette;

    public IEnumerable<double> ThicknessOptions => _thicknessOptions;

    public ToolKind ActiveTool => _session.ActiveTool;

    public Color SelectedColor => _session.SelectedColor;

    public double SelectedThickness => _session.SelectedThickness;

    public bool IsPenActive => ActiveTool == ToolKind.Pen && _session.OverlayVisible;

    public bool IsEraserActive => ActiveTool == ToolKind.Eraser && _session.OverlayVisible;

    public bool IsDrawingModeVisible => _session.OverlayVisible;

    private void SelectPen()
    {
        if (_session.ActiveTool == ToolKind.Pen && _session.OverlayVisible)
        {
            _services.OverlayWindowService.Hide();
            RaiseToolbarState();
            return;
        }

        _session.ActiveTool = ToolKind.Pen;
        _session.OverlayVisible = true;
        EnsureOverlay();
        RaiseToolbarState();
    }

    private void SelectEraser()
    {
        if (_session.ActiveTool == ToolKind.Eraser && _session.OverlayVisible)
        {
            _services.OverlayWindowService.Hide();
            RaiseToolbarState();
            return;
        }

        _session.ActiveTool = ToolKind.Eraser;
        _session.OverlayVisible = true;
        EnsureOverlay();
        RaiseToolbarState();
    }

    private void Undo()
    {
        _session.Undo();
        RaiseToolbarState();
    }

    private void Redo()
    {
        _session.Redo();
        RaiseToolbarState();
    }

    private void SaveDrawing()
    {
        try
        {
            if (_session.BackgroundSnapshot is null)
            {
                MessageBox.Show("Kaydedilecek aktif bir çizim oturumu bulunmuyor.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var path = _services.FileExportService.SaveCompositePng(_session, _session.Settings);
            MessageBox.Show($"PNG kaydedildi:\n{path}", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Kaydetme işlemi başarısız oldu.", ex);
            MessageBox.Show("PNG kaydedilirken bir hata oluştu.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenSettings()
    {
        try
        {
            if (Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault() is SettingsWindow existing)
            {
                existing.Activate();
                return;
            }

            var window = new SettingsWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = new SettingsViewModel(_services)
            };
            window.Show();
            RaiseToolbarState();
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Ayarlar penceresi açılamadı.", ex);
            MessageBox.Show("Ayarlar penceresi açılamadı. Ayrıntılar log dosyasına yazıldı.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SelectColor(object? parameter)
    {
        if (parameter is Color color)
        {
            _session.SelectedColor = color;
            _session.ActiveTool = ToolKind.Pen;
            _session.OverlayVisible = true;
            EnsureOverlay();
            RaiseToolbarState();
        }
    }

    private void SelectThickness(object? parameter)
    {
        if (parameter is double thickness)
        {
            _session.SelectedThickness = thickness;
            RaiseToolbarState();
        }
    }

    private OverlayWindow EnsureOverlay()
    {
        var owner = Application.Current.MainWindow!;
        return _services.OverlayWindowService.EnsureVisible(owner);
    }

    private void RaiseToolbarState()
    {
        RaisePropertyChanged(nameof(ActiveTool));
        RaisePropertyChanged(nameof(SelectedColor));
        RaisePropertyChanged(nameof(SelectedThickness));
        RaisePropertyChanged(nameof(IsPenActive));
        RaisePropertyChanged(nameof(IsEraserActive));
        RaisePropertyChanged(nameof(IsDrawingModeVisible));
    }

}
