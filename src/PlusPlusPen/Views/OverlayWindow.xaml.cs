using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using PlusPlusPen.Controls;
using PlusPlusPen.Services;

namespace PlusPlusPen.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow(DrawingSessionService sessionService)
    {
        InitializeComponent();
        DataContext = sessionService;
        IsHitTestVisible = true;
        SyncBounds();
    }

    public DrawingSurfaceControl SurfaceHost => DrawingSurface;

    public void SyncBounds()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(
            0,
            0,
            (int)SystemParameters.PrimaryScreenWidth,
            (int)SystemParameters.PrimaryScreenHeight);
        var dpi = VisualTreeHelper.GetDpi(this);

        Left = bounds.Left / dpi.DpiScaleX;
        Top = bounds.Top / dpi.DpiScaleY;
        Width = bounds.Width / dpi.DpiScaleX;
        Height = bounds.Height / dpi.DpiScaleY;
    }

    public void ApplySettings(Models.AppSettingsModel settings)
    {
        Opacity = 1.0;
    }

    public void PrepareForInput()
    {
        DrawingSurface.Focus();
    }
}
