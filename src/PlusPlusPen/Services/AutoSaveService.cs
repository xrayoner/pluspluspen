using System.Windows.Threading;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class AutoSaveService
{
    private readonly DispatcherTimer _timer = new();
    private readonly FileExportService _fileExportService;
    private readonly DrawingSessionService _drawingSessionService;
    private readonly LogService _logService;

    public AutoSaveService(
        FileExportService fileExportService,
        DrawingSessionService drawingSessionService,
        LogService logService)
    {
        _fileExportService = fileExportService;
        _drawingSessionService = drawingSessionService;
        _logService = logService;
        _timer.Tick += HandleTick;
    }

    public AppSettingsModel? CurrentSettings { get; private set; }

    public void Apply(AppSettingsModel settings)
    {
        CurrentSettings = settings;
        _timer.Stop();

        if (!settings.AutoSaveEnabled)
        {
            return;
        }

        _timer.Interval = settings.AutoSaveInterval switch
        {
            AutoSaveIntervalOption.OneMinute => TimeSpan.FromMinutes(1),
            AutoSaveIntervalOption.TenMinutes => TimeSpan.FromMinutes(10),
            _ => TimeSpan.FromMinutes(5)
        };

        _timer.Start();
    }

    private void HandleTick(object? sender, EventArgs e)
    {
        if (CurrentSettings is null)
        {
            return;
        }

        try
        {
            if (!_drawingSessionService.OverlayVisible || _drawingSessionService.BackgroundSnapshot is null)
            {
                return;
            }

            _fileExportService.SaveCompositePng(_drawingSessionService, CurrentSettings);
            _logService.LogInfo("Otomatik kaydetme tamamlandı.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Otomatik kaydetme başarısız oldu.", ex);
        }
    }
}
