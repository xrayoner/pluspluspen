using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PlusPlusPen.Views;

namespace PlusPlusPen.Services;

public sealed class OverlayWindowService
{
    private readonly DrawingSessionService _sessionService;
    private readonly LogService _logService;
    private OverlayWindow? _overlayWindow;
    private Task? _pendingCaptureTask;

    public OverlayWindowService(DrawingSessionService sessionService, LogService logService)
    {
        _sessionService = sessionService;
        _logService = logService;
    }

    public OverlayWindow EnsureVisible(Window owner)
    {
        _overlayWindow ??= new OverlayWindow(_sessionService);
        var requiresCapture = _sessionService.BackgroundSnapshot is null;
        _sessionService.BeginOverlaySession();

        if (!_overlayWindow.IsVisible)
        {
            _overlayWindow.SyncBounds();
            _overlayWindow.Topmost = true;
            _overlayWindow.Show();
            _overlayWindow.PrepareForInput();
        }

        _sessionService.OverlayVisible = true;
        _overlayWindow.SyncBounds();
        _overlayWindow.ApplySettings(_sessionService.Settings);
        _overlayWindow.Topmost = true;
        owner.Show();
        RefreshToolbarZOrder(owner);
        if (requiresCapture)
        {
            ScheduleBackgroundCapture(owner);
        }
        else
        {
            _overlayWindow.PrepareForInput();
            RefreshToolbarZOrder(owner);
        }
        return _overlayWindow;
    }

    public OverlayWindow? GetWindow() => _overlayWindow;

    public void PrepareForExistingSession()
    {
        if (_overlayWindow?.IsVisible == true)
        {
            _overlayWindow.SyncBounds();
            _overlayWindow.PrepareForInput();
            if (System.Windows.Application.Current.MainWindow is Window toolbar)
            {
                RefreshToolbarZOrder(toolbar);
            }
        }
    }

    public void Hide()
    {
        _sessionService.OverlayVisible = false;
        _overlayWindow?.Hide();
        _sessionService.ResetOverlaySurface();
    }

    public void ApplySettings(Window owner)
    {
        if (_overlayWindow is not null)
        {
            _overlayWindow.ApplySettings(_sessionService.Settings);
            _overlayWindow.Topmost = true;
        }

        RefreshToolbarZOrder(owner);
    }

    private void ScheduleBackgroundCapture(Window owner)
    {
        if (_sessionService.BackgroundSnapshot is not null)
        {
            return;
        }

        if (_pendingCaptureTask is { IsCompleted: false })
        {
            return;
        }

        _pendingCaptureTask = CaptureAndInitializeAsync(owner);
    }

    private async Task CaptureAndInitializeAsync(Window owner)
    {
        await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        var originalOpacity = 1d;
        var originalTopmost = owner.Topmost;
        var originalHitTestVisible = true;

        try
        {
            await owner.Dispatcher.InvokeAsync(() =>
            {
                originalOpacity = owner.Opacity;
                originalHitTestVisible = owner.IsHitTestVisible;
                owner.Topmost = false;
                owner.Opacity = 0;
                owner.IsHitTestVisible = false;
            }, DispatcherPriority.Send);

            await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            await Task.Delay(80);

            var stopwatch = Stopwatch.StartNew();
            var snapshot = await Task.Run(CapturePrimaryScreen);
            stopwatch.Stop();
            LogDebug($"Screenshot capture: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

            await owner.Dispatcher.InvokeAsync(() =>
            {
                _sessionService.SetBackgroundSnapshot(snapshot);
                _overlayWindow?.InvalidateVisual();
            }, DispatcherPriority.Send);
        }
        catch (Exception ex)
        {
            _logService.LogError("Ekran görüntüsü alınamadı.", ex);
        }
        finally
        {
            await owner.Dispatcher.InvokeAsync(() =>
            {
                owner.Opacity = originalOpacity;
                owner.IsHitTestVisible = originalHitTestVisible;
                owner.Topmost = originalTopmost;
                RefreshToolbarZOrder(owner);
            }, DispatcherPriority.Send);
        }
    }

    private void RefreshToolbarZOrder(Window owner)
    {
        if (_overlayWindow is { IsVisible: true })
        {
            var overlayHandle = new WindowInteropHelper(_overlayWindow).Handle;
            if (overlayHandle != IntPtr.Zero)
            {
                SetWindowPos(
                    overlayHandle,
                    HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        var ownerHandle = new WindowInteropHelper(owner).Handle;
        if (ownerHandle != IntPtr.Zero)
        {
            SetWindowPos(
                ownerHandle,
                HWND_TOPMOST,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        owner.Topmost = false;
        owner.Topmost = true;
        owner.Activate();
        owner.Focus();
    }

    private static BitmapSource CapturePrimaryScreen()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(
            0,
            0,
            (int)SystemParameters.PrimaryScreenWidth,
            (int)SystemParameters.PrimaryScreenHeight);
        var left = bounds.Left;
        var top = bounds.Top;
        var width = Math.Max(1, bounds.Width);
        var height = Math.Max(1, bounds.Height);

        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }

        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

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
        Debug.WriteLine($"[++PEN][Capture] {message}");
    }
}
