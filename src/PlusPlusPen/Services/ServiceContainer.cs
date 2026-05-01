namespace PlusPlusPen.Services;

public sealed class ServiceContainer
{
    private ServiceContainer()
    {
    }

    public LogService LogService { get; private init; } = null!;

    public AppSettingsService AppSettingsService { get; private init; } = null!;

    public DrawingSessionService DrawingSessionService { get; private init; } = null!;

    public OverlayWindowService OverlayWindowService { get; private init; } = null!;

    public FileExportService FileExportService { get; private init; } = null!;

    public AppThemeService AppThemeService { get; private init; } = null!;

    public UpdatePackageService UpdatePackageService { get; private init; } = null!;

    public AutoSaveService AutoSaveService { get; private init; } = null!;

    public static ServiceContainer Create()
    {
        var logService = new LogService();
        var settingsService = new AppSettingsService(logService);
        var sessionService = new DrawingSessionService(settingsService.Load());
        var exportService = new FileExportService();
        var overlayService = new OverlayWindowService(sessionService, logService);
        var themeService = new AppThemeService();
        var updatePackageService = new UpdatePackageService(logService);
        var autoSaveService = new AutoSaveService(exportService, sessionService, logService);

        themeService.Apply(sessionService.Settings);
        autoSaveService.Apply(sessionService.Settings);

        return new ServiceContainer
        {
            LogService = logService,
            AppSettingsService = settingsService,
            DrawingSessionService = sessionService,
            FileExportService = exportService,
            OverlayWindowService = overlayService,
            AppThemeService = themeService,
            UpdatePackageService = updatePackageService,
            AutoSaveService = autoSaveService
        };
    }
}
