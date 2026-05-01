using System.Drawing;
using System.Runtime.InteropServices;
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

    public OverlayWindowService(DrawingSessionService sessionService, LogService logService)
    {
        _sessionService = sessionService;
        _logService = logService;
    }

    public OverlayWindow EnsureVisible(Window owner)
    {
        _overlayWindow ??= new OverlayWindow(_sessionService);

        if (!_overlayWindow.IsVisible)
        {
            CaptureAndInitialize(owner);
            _overlayWindow.SyncBounds();
            _overlayWindow.Topmost = false;
            _overlayWindow.Show();
        }

        _sessionService.OverlayVisible = true;
        _overlayWindow.SyncBounds();
        _overlayWindow.ApplySettings(_sessionService.Settings);
        _overlayWindow.Topmost = false;
        owner.Show();
        RefreshToolbarZOrder(owner);
        return _overlayWindow;
    }

    public OverlayWindow? GetWindow() => _overlayWindow;

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
            _overlayWindow.Topmost = false;
        }

        RefreshToolbarZOrder(owner);
    }

    private void CaptureAndInitialize(Window owner)
    {
        owner.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

        try
        {
            _sessionService.InitializeOverlaySurface(CapturePrimaryScreen());
        }
        catch (Exception ex)
        {
            _logService.LogError("Ekran görüntüsü alınamadı.", ex);
            throw;
        }
    }

    private static void RefreshToolbarZOrder(Window owner)
    {
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
}
