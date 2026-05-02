using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PlusPlusPen.Helpers;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class FileExportService
{
    public string SaveCompositePng(DrawingSessionService session, AppSettingsModel settings)
    {
        var snapshot = session.BackgroundSnapshot;
        if (snapshot is null)
        {
            throw new InvalidOperationException("Kaydedilecek ekran görüntüsü bulunamadı.");
        }

        var width = Math.Max(1, snapshot.PixelWidth);
        var height = Math.Max(1, snapshot.PixelHeight);
        var scaleX = width / Math.Max(1.0, SystemParameters.PrimaryScreenWidth);
        var scaleY = height / Math.Max(1.0, SystemParameters.PrimaryScreenHeight);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(snapshot, new Rect(0, 0, width, height));

            foreach (var stroke in session.Strokes)
            {
                StrokeRenderHelper.DrawStroke(
                    context,
                    stroke,
                    scaleX,
                    scaleY,
                    StrokeRenderHelper.CreateRenderOptions(settings, isLivePreview: false));
            }
        }

        var render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        render.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(render));

        var folder = Directory.Exists(settings.SaveFolder)
            ? settings.SaveFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        Directory.CreateDirectory(folder);
        var fileName = $"pluspluspen_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var path = Path.Combine(folder, fileName);

        using var stream = File.Create(path);
        encoder.Save(stream);

        return path;
    }
}
