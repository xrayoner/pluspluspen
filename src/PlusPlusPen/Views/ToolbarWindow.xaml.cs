using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PlusPlusPen.Models;
using PlusPlusPen.Services;
using PlusPlusPen.ViewModels;

namespace PlusPlusPen.Views;

public partial class ToolbarWindow : Window
{
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmMouseMove = 0x0200;
    private const int MkLButton = 0x0001;
    private readonly ServiceContainer? _services;
    private bool _titleMouseDown;
    private Point _titleDownPoint;

    public ToolbarWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Left + 30;
        Top = SystemParameters.WorkArea.Top + 60;
        SourceInitialized += HandleSourceInitialized;
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

        PanelRoot.Width = settings.PanelSize switch
        {
            PanelSizeOption.Small => 132,
            PanelSizeOption.Large => 156,
            _ => 142
        };
    }

    private void HandleSourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(HandleWindowMessage);
        }
    }

    private IntPtr HandleWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var point = GetClientPoint(lParam);
        switch (message)
        {
            case WmLButtonDown when IsInTitleArea(point):
                _titleMouseDown = true;
                _titleDownPoint = point;
                handled = true;
                break;
            case WmMouseMove when _titleMouseDown && (((int)wParam & MkLButton) == MkLButton):
                if (Math.Abs(point.X - _titleDownPoint.X) >= SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(point.Y - _titleDownPoint.Y) >= SystemParameters.MinimumVerticalDragDistance)
                {
                    _titleMouseDown = false;
                    handled = true;
                    DragMove();
                }
                break;
            case WmLButtonUp when _titleMouseDown:
                _titleMouseDown = false;
                handled = true;
                Dispatcher.BeginInvoke(OpenSettingsFromLogo);
                break;
        }

        return IntPtr.Zero;
    }

    private Point GetClientPoint(IntPtr lParam)
    {
        var x = unchecked((short)((long)lParam & 0xFFFF));
        var y = unchecked((short)(((long)lParam >> 16) & 0xFFFF));
        return new Point(x, y);
    }

    private bool IsInTitleArea(Point point)
    {
        return point.X >= 0 && point.Y >= 0 && point.Y <= 34;
    }

    private void OpenSettingsFromLogo()
    {
        if (_services is null)
        {
            return;
        }

        try
        {
            if (Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault() is SettingsWindow existing)
            {
                existing.Activate();
                return;
            }

            var window = new SettingsWindow
            {
                Owner = this,
                DataContext = new SettingsViewModel(_services)
            };
            window.Show();
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Ayarlar penceresi açılamadı.", ex);
            MessageBox.Show("Ayarlar penceresi açılamadı. Ayrıntılar log dosyasına yazıldı.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void HandlePanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        if (e.LeftButton != MouseButtonState.Pressed
            || IsInsideButton(source)
            || IsInsideElement(source, TitleDragArea))
        {
            return;
        }

        DragMove();
        e.Handled = true;
    }

    private static bool IsInsideButton(DependencyObject? source)
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

    private static bool IsInsideElement(DependencyObject? source, DependencyObject target)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
