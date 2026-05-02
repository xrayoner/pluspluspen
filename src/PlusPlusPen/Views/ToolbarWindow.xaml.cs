using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PlusPlusPen.Models;
using PlusPlusPen.Services;
using PlusPlusPen.ViewModels;

namespace PlusPlusPen.Views;

public partial class ToolbarWindow : Window
{
    private readonly ServiceContainer? _services;
    private bool _dragPending;
    private Point _dragStartPoint;

    public ToolbarWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Left + 30;
        Top = SystemParameters.WorkArea.Top + 60;
    }

    public ToolbarWindow(ServiceContainer services)
        : this()
    {
        _services = services;
    }

    public void ApplySettings(AppSettingsModel settings)
    {
        Opacity = Math.Clamp(settings.PanelOpacity, 0.2, 1.0);
        Topmost = settings.KeepPanelOnTop;

        var scale = settings.PanelSize switch
        {
            PanelSizeOption.Small => 0.9,
            PanelSizeOption.Large => 1.18,
            _ => 1.0
        };

        PanelScale.ScaleX = scale;
        PanelScale.ScaleY = scale;
    }

    private void HandleToolbarLoaded(object sender, RoutedEventArgs e)
    {
        LogDebug("Toolbar loaded");
    }

    private void HandleWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
            && e.Key == Key.OemComma)
        {
            OpenSettingsWindow();
            e.Handled = true;
        }
    }

    private void DragSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveSource(e.OriginalSource as DependencyObject))
        {
            return;
        }

        LogDebug("Drag surface mouse down");
        _dragPending = true;
        _dragStartPoint = e.GetPosition(this);
        DragSurface.CaptureMouse();
        e.Handled = true;
    }

    private void DragSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragPending || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(point.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dragPending = false;
        DragSurface.ReleaseMouseCapture();
        DragMove();
        e.Handled = true;
    }

    private void DragSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragPending = false;
        DragSurface.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
    {
        if (_services is null)
        {
            return;
        }

        try
        {
            if (Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault() is SettingsWindow existing)
            {
                if (existing.WindowState == WindowState.Minimized)
                {
                    existing.WindowState = WindowState.Normal;
                }

                existing.Activate();
                existing.Focus();
                return;
            }

            var window = new SettingsWindow
            {
                Owner = this,
                DataContext = new SettingsViewModel(_services)
            };
            window.Show();
            window.Activate();
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Ayarlar penceresi acilamadi.", ex);
            MessageBox.Show("Ayarlar penceresi acilamadi. Ayrintilar log dosyasina yazildi.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static bool IsInteractiveSource(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    [Conditional("DEBUG")]
    private static void LogDebug(string message)
    {
        Debug.WriteLine($"[++PEN][Input] {message}");
    }
}
