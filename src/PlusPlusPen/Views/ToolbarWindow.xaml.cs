using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using PlusPlusPen.Models;
using PlusPlusPen.Services;
using PlusPlusPen.ViewModels;

namespace PlusPlusPen.Views;

public partial class ToolbarWindow : Window
{
    private readonly ServiceContainer? _services;
    private bool _dragPending;
    private Point _dragStartPoint;
    private ContextMenu? _penSizeMenu;
    private ContextMenu? _eraserSizeMenu;
    private DispatcherTimer? _pendingPenToggleTimer;
    private DispatcherTimer? _pendingEraserToggleTimer;

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

    private void HandleToolbarDeactivated(object? sender, EventArgs e)
    {
        if (_services is null || !_services.DrawingSessionService.IsDrawingSessionActive)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (!_services.DrawingSessionService.IsDrawingSessionActive || !IsVisible)
            {
                return;
            }

            KeepToolbarTopmostWithoutActivation();
        }, DispatcherPriority.Background);
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

    private void PenButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ToolbarViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsPenActive && e.ClickCount == 1)
        {
            SchedulePendingToolToggle(
                ref _pendingPenToggleTimer,
                () => viewModel.SelectPenCommand.Execute(null));
            e.Handled = true;
            return;
        }

        if (e.ClickCount < 2)
        {
            return;
        }

        CancelPendingToolToggle(ref _pendingPenToggleTimer);
        LogDebug("Pen button double click");
        EnsurePenSizeMenu().IsOpen = true;
        e.Handled = true;
    }

    private void EraserButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ToolbarViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsEraserActive && e.ClickCount == 1)
        {
            SchedulePendingToolToggle(
                ref _pendingEraserToggleTimer,
                () => viewModel.SelectEraserCommand.Execute(null));
            e.Handled = true;
            return;
        }

        if (e.ClickCount < 2)
        {
            return;
        }

        CancelPendingToolToggle(ref _pendingEraserToggleTimer);
        LogDebug("Eraser button double click");
        EnsureEraserSizeMenu().IsOpen = true;
        e.Handled = true;
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

    private static void SchedulePendingToolToggle(ref DispatcherTimer? timer, Action action)
    {
        CancelPendingToolToggle(ref timer);
        var localTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime + 20)
        };
        localTimer.Tick += (_, _) =>
        {
            localTimer.Stop();
            action();
        };
        timer = localTimer;
        localTimer.Start();
    }

    private static void CancelPendingToolToggle(ref DispatcherTimer? timer)
    {
        if (timer is null)
        {
            return;
        }

        timer.Stop();
        timer = null;
    }

    private ContextMenu EnsurePenSizeMenu()
    {
        if (_penSizeMenu is not null)
        {
            _penSizeMenu.PlacementTarget = PenButton;
            return _penSizeMenu;
        }

        _penSizeMenu = BuildSizeMenu(
            PenButton,
            "Kalem kalınlığı",
            [
                ("İnce", 4d),
                ("Orta", 7d),
                ("Kalın", 11d),
                ("Çok Kalın", 16d)
            ],
            value =>
            {
                if (_services is null)
                {
                    return;
                }

                _services.DrawingSessionService.SelectedThickness = value;
                if (DataContext is ToolbarViewModel viewModel)
                {
                    viewModel.SelectThicknessCommand.Execute(value);
                }
            });

        return _penSizeMenu;
    }

    private ContextMenu EnsureEraserSizeMenu()
    {
        if (_eraserSizeMenu is not null)
        {
            _eraserSizeMenu.PlacementTarget = EraserButton;
            return _eraserSizeMenu;
        }

        _eraserSizeMenu = BuildSizeMenu(
            EraserButton,
            "Silgi boyutu",
            [
                ("Küçük", 12d),
                ("Orta", 20d),
                ("Büyük", 30d),
                ("Çok Büyük", 42d)
            ],
            value =>
            {
                if (_services is null)
                {
                    return;
                }

                _services.DrawingSessionService.Settings.EraserSize = value;
                _services.DrawingSessionService.RaiseStateSignals();
            });

        return _eraserSizeMenu;
    }

    private static ContextMenu BuildSizeMenu(
        UIElement placementTarget,
        string header,
        IReadOnlyList<(string Label, double Value)> options,
        Action<double> apply)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = placementTarget,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(120, 198, 246)),
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(new MenuItem
        {
            Header = header,
            IsEnabled = false,
            FontWeight = FontWeights.SemiBold
        });

        foreach (var option in options)
        {
            var item = new MenuItem { Header = option.Label };
            item.Click += (_, _) => apply(option.Value);
            menu.Items.Add(item);
        }

        return menu;
    }

    private void KeepToolbarTopmostWithoutActivation()
    {
        Topmost = true;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(
            handle,
            HWND_TOPMOST,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [Conditional("DEBUG")]
    private static void LogDebug(string message)
    {
        Debug.WriteLine($"[++PEN][Input] {message}");
    }
}
