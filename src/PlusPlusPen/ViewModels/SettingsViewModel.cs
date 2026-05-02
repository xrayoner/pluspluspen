using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PlusPlusPen.Models;
using PlusPlusPen.Services;
using Forms = System.Windows.Forms;

namespace PlusPlusPen.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ServiceContainer _services;
    private readonly DrawingSessionService _session;
    private bool _launchAtStartup;
    private bool _keepPanelOnTop;
    private double _panelOpacityPercent;
    private PanelSizeOption _panelSize;
    private ToolKind _defaultTool;
    private AppLanguage _language;
    private ThemeMode _selectedTheme;
    private string _accentColorHex;
    private string _activeToolColorHex;
    private double _defaultThickness;
    private PenStyleOption _penStyle;
    private PenSensitivity _penSensitivity;
    private SmoothingPresetOption _smoothingPreset;
    private bool _dynamicThicknessEnabled;
    private bool _velocityBasedThicknessEnabled;
    private bool _strokeTaperEnabled;
    private bool _autoStraightLineEnabled;
    private double _straightLineSensitivity;
    private bool _fountainPenEffectEnabled;
    private bool _performanceModeEnabled;
    private bool _liveSmoothingEnabled;
    private bool _smoothingEnabled;
    private double _smoothingLevel;
    private double _minimumPointDistance;
    private int _interpolationLimit;
    private double _minimumStrokeThickness;
    private double _maximumStrokeThickness;
    private bool _mouseSpeedAffectsThickness;
    private bool _stylusPressureEnabled;
    private bool _palmRejectionPlanned;
    private double _eraserSize;
    private EraserMode _eraserMode;
    private string _saveFolder;
    private bool _transparentBackgroundEnabled;
    private bool _autoSaveEnabled;
    private AutoSaveIntervalOption _autoSaveInterval;
    private string _manifestVersion;
    private string _manifestMinVersion;
    private string _manifestNotes;
    private string _manifestDownloadUrl;
    private string _manifestSha256;
    private string _lastUpdatePackagePath;

    public SettingsViewModel(ServiceContainer services)
    {
        _services = services;
        _session = services.DrawingSessionService;

        var settings = _session.Settings;
        _launchAtStartup = settings.LaunchAtStartup;
        _keepPanelOnTop = settings.KeepPanelOnTop;
        _panelOpacityPercent = settings.PanelOpacity * 100;
        _panelSize = settings.PanelSize;
        _defaultTool = settings.DefaultTool;
        _language = settings.Language;
        _selectedTheme = settings.Theme;
        _accentColorHex = settings.AccentColorHex;
        _activeToolColorHex = settings.ActiveToolColorHex;
        _defaultThickness = settings.DefaultThickness;
        _penStyle = settings.PenStyle;
        _penSensitivity = settings.PenSensitivity;
        _smoothingPreset = settings.SmoothingPreset;
        _dynamicThicknessEnabled = settings.DynamicThicknessEnabled;
        _velocityBasedThicknessEnabled = settings.VelocityBasedThicknessEnabled;
        _strokeTaperEnabled = settings.StrokeTaperEnabled;
        _autoStraightLineEnabled = settings.AutoStraightLineEnabled;
        _straightLineSensitivity = settings.StraightLineSensitivity * 100;
        _fountainPenEffectEnabled = settings.FountainPenEffectEnabled;
        _performanceModeEnabled = settings.PerformanceModeEnabled;
        _liveSmoothingEnabled = settings.LiveSmoothingEnabled;
        _smoothingEnabled = settings.SmoothingEnabled;
        _smoothingLevel = settings.SmoothingLevel * 100;
        _minimumPointDistance = settings.MinimumPointDistance;
        _interpolationLimit = settings.InterpolationLimit;
        _minimumStrokeThickness = settings.MinimumStrokeThickness;
        _maximumStrokeThickness = settings.MaximumStrokeThickness;
        _mouseSpeedAffectsThickness = settings.MouseSpeedAffectsThickness;
        _stylusPressureEnabled = settings.StylusPressureEnabled;
        _palmRejectionPlanned = settings.PalmRejectionPlanned;
        _eraserSize = settings.EraserSize;
        _eraserMode = settings.EraserMode;
        _saveFolder = settings.SaveFolder;
        _transparentBackgroundEnabled = settings.TransparentBackgroundEnabled;
        _autoSaveEnabled = settings.AutoSaveEnabled;
        _autoSaveInterval = settings.AutoSaveInterval;
        _lastUpdatePackagePath = settings.LastUpdatePackagePath;
        _manifestVersion = "Henüz paket seçilmedi";
        _manifestMinVersion = "-";
        _manifestNotes = "Güncelleştirme Merkezi hazır.";
        _manifestDownloadUrl = "-";
        _manifestSha256 = "-";

        ThemeOptions = new ObservableCollection<ThemeMode>(Enum.GetValues<ThemeMode>());
        PanelSizeOptions = new ObservableCollection<PanelSizeOption>(Enum.GetValues<PanelSizeOption>());
        ToolOptions = new ObservableCollection<ToolKind>(Enum.GetValues<ToolKind>().Where(x => x != ToolKind.None));
        LanguageOptions = new ObservableCollection<AppLanguage>(Enum.GetValues<AppLanguage>());
        PenStyleOptions = new ObservableCollection<PenStyleOption>(Enum.GetValues<PenStyleOption>());
        PenSensitivityOptions = new ObservableCollection<PenSensitivity>(Enum.GetValues<PenSensitivity>());
        SmoothingPresetOptions = new ObservableCollection<SmoothingPresetOption>(Enum.GetValues<SmoothingPresetOption>());
        EraserModeOptions = new ObservableCollection<EraserMode>(Enum.GetValues<EraserMode>());
        AutoSaveIntervalOptions = new ObservableCollection<AutoSaveIntervalOption>(Enum.GetValues<AutoSaveIntervalOption>());

        SaveCommand = new RelayCommand(Save);
        BrowseSaveFolderCommand = new RelayCommand(BrowseSaveFolder);
        FetchUpdateFromInternetCommand = new RelayCommand(FetchUpdateFromInternet);
        LoadUpdateFromFileCommand = new RelayCommand(LoadUpdateFromFile);
        ClearCanvasCommand = new RelayCommand(ClearCanvas);
        ResetSettingsCommand = new RelayCommand(ResetSettings);
        ClearCacheCommand = new RelayCommand(ClearCache);
        OpenLogCommand = new RelayCommand(OpenLog);
        ClearLogsCommand = new RelayCommand(ClearLogs);
        ShowUninstallInfoCommand = new RelayCommand(ShowUninstallInfo);
    }

    public ObservableCollection<ThemeMode> ThemeOptions { get; }

    public ObservableCollection<PanelSizeOption> PanelSizeOptions { get; }

    public ObservableCollection<ToolKind> ToolOptions { get; }

    public ObservableCollection<AppLanguage> LanguageOptions { get; }

    public ObservableCollection<PenStyleOption> PenStyleOptions { get; }

    public ObservableCollection<PenSensitivity> PenSensitivityOptions { get; }

    public ObservableCollection<SmoothingPresetOption> SmoothingPresetOptions { get; }

    public ObservableCollection<EraserMode> EraserModeOptions { get; }

    public ObservableCollection<AutoSaveIntervalOption> AutoSaveIntervalOptions { get; }

    public ICommand SaveCommand { get; }

    public ICommand BrowseSaveFolderCommand { get; }

    public ICommand FetchUpdateFromInternetCommand { get; }

    public ICommand LoadUpdateFromFileCommand { get; }

    public ICommand ClearCanvasCommand { get; }

    public ICommand ResetSettingsCommand { get; }

    public ICommand ClearCacheCommand { get; }

    public ICommand OpenLogCommand { get; }

    public ICommand ClearLogsCommand { get; }

    public ICommand ShowUninstallInfoCommand { get; }

    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => SetProperty(ref _launchAtStartup, value);
    }

    public bool KeepPanelOnTop
    {
        get => _keepPanelOnTop;
        set => SetProperty(ref _keepPanelOnTop, value);
    }

    public double PanelOpacityPercent
    {
        get => _panelOpacityPercent;
        set => SetProperty(ref _panelOpacityPercent, value);
    }

    public PanelSizeOption PanelSize
    {
        get => _panelSize;
        set => SetProperty(ref _panelSize, value);
    }

    public ToolKind DefaultTool
    {
        get => _defaultTool;
        set => SetProperty(ref _defaultTool, value);
    }

    public AppLanguage Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    public string AccentColorHex
    {
        get => _accentColorHex;
        set => SetProperty(ref _accentColorHex, value);
    }

    public string ActiveToolColorHex
    {
        get => _activeToolColorHex;
        set => SetProperty(ref _activeToolColorHex, value);
    }

    public double DefaultThickness
    {
        get => _defaultThickness;
        set => SetProperty(ref _defaultThickness, value);
    }

    public PenStyleOption PenStyle
    {
        get => _penStyle;
        set => SetProperty(ref _penStyle, value);
    }

    public PenSensitivity PenSensitivity
    {
        get => _penSensitivity;
        set => SetProperty(ref _penSensitivity, value);
    }

    public SmoothingPresetOption SmoothingPreset
    {
        get => _smoothingPreset;
        set => SetProperty(ref _smoothingPreset, value);
    }

    public bool DynamicThicknessEnabled
    {
        get => _dynamicThicknessEnabled;
        set => SetProperty(ref _dynamicThicknessEnabled, value);
    }

    public bool VelocityBasedThicknessEnabled
    {
        get => _velocityBasedThicknessEnabled;
        set => SetProperty(ref _velocityBasedThicknessEnabled, value);
    }

    public bool StrokeTaperEnabled
    {
        get => _strokeTaperEnabled;
        set => SetProperty(ref _strokeTaperEnabled, value);
    }

    public bool AutoStraightLineEnabled
    {
        get => _autoStraightLineEnabled;
        set => SetProperty(ref _autoStraightLineEnabled, value);
    }

    public double StraightLineSensitivity
    {
        get => _straightLineSensitivity;
        set => SetProperty(ref _straightLineSensitivity, value);
    }

    public bool FountainPenEffectEnabled
    {
        get => _fountainPenEffectEnabled;
        set => SetProperty(ref _fountainPenEffectEnabled, value);
    }

    public bool PerformanceModeEnabled
    {
        get => _performanceModeEnabled;
        set => SetProperty(ref _performanceModeEnabled, value);
    }

    public bool LiveSmoothingEnabled
    {
        get => _liveSmoothingEnabled;
        set => SetProperty(ref _liveSmoothingEnabled, value);
    }

    public bool SmoothingEnabled
    {
        get => _smoothingEnabled;
        set => SetProperty(ref _smoothingEnabled, value);
    }

    public double SmoothingLevel
    {
        get => _smoothingLevel;
        set => SetProperty(ref _smoothingLevel, value);
    }

    public double MinimumPointDistance
    {
        get => _minimumPointDistance;
        set => SetProperty(ref _minimumPointDistance, value);
    }

    public int InterpolationLimit
    {
        get => _interpolationLimit;
        set => SetProperty(ref _interpolationLimit, value);
    }

    public double MinimumStrokeThickness
    {
        get => _minimumStrokeThickness;
        set => SetProperty(ref _minimumStrokeThickness, value);
    }

    public double MaximumStrokeThickness
    {
        get => _maximumStrokeThickness;
        set => SetProperty(ref _maximumStrokeThickness, value);
    }

    public bool MouseSpeedAffectsThickness
    {
        get => _mouseSpeedAffectsThickness;
        set => SetProperty(ref _mouseSpeedAffectsThickness, value);
    }

    public bool StylusPressureEnabled
    {
        get => _stylusPressureEnabled;
        set => SetProperty(ref _stylusPressureEnabled, value);
    }

    public bool PalmRejectionPlanned
    {
        get => _palmRejectionPlanned;
        set => SetProperty(ref _palmRejectionPlanned, value);
    }

    public double EraserSize
    {
        get => _eraserSize;
        set => SetProperty(ref _eraserSize, value);
    }

    public EraserMode EraserMode
    {
        get => _eraserMode;
        set => SetProperty(ref _eraserMode, value);
    }

    public string SaveFolder
    {
        get => _saveFolder;
        set => SetProperty(ref _saveFolder, value);
    }

    public bool TransparentBackgroundEnabled
    {
        get => _transparentBackgroundEnabled;
        set => SetProperty(ref _transparentBackgroundEnabled, value);
    }

    public bool AutoSaveEnabled
    {
        get => _autoSaveEnabled;
        set => SetProperty(ref _autoSaveEnabled, value);
    }

    public AutoSaveIntervalOption AutoSaveInterval
    {
        get => _autoSaveInterval;
        set => SetProperty(ref _autoSaveInterval, value);
    }

    public string CurrentVersion => AppVersionInfo.CurrentDisplayVersion;

    public string ManifestVersion
    {
        get => _manifestVersion;
        set => SetProperty(ref _manifestVersion, value);
    }

    public string ManifestMinVersion
    {
        get => _manifestMinVersion;
        set => SetProperty(ref _manifestMinVersion, value);
    }

    public string ManifestNotes
    {
        get => _manifestNotes;
        set => SetProperty(ref _manifestNotes, value);
    }

    public string ManifestDownloadUrl
    {
        get => _manifestDownloadUrl;
        set => SetProperty(ref _manifestDownloadUrl, value);
    }

    public string ManifestSha256
    {
        get => _manifestSha256;
        set => SetProperty(ref _manifestSha256, value);
    }

    public string LastUpdatePackagePath
    {
        get => _lastUpdatePackagePath;
        set => SetProperty(ref _lastUpdatePackagePath, value);
    }

    public string FileNamePattern => "pluspluspen_YYYYMMDD_HHMMSS";

    public string PalmRejectionStatus => "Yakında";

    public string UpdateFeatureNote => "Güncelleştirme Merkezi paketi doğrular, ++PEN’i kapatır, yeni dosyaları kurar ve uygulamayı yeniden başlatır.";

    private void Save()
    {
        try
        {
            var settings = BuildSettingsModel();
            _services.AppSettingsService.Save(settings);
            _session.ApplySettings(settings);
            _services.AppThemeService.Apply(settings);
            _services.AutoSaveService.Apply(settings);

            if (Application.Current.MainWindow is Views.ToolbarWindow toolbar)
            {
                toolbar.ApplySettings(settings);
                _services.OverlayWindowService.ApplySettings(toolbar);
            }

            MessageBox.Show("Ayarlar kaydedildi.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseWindow();
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Ayarlar kaydedilemedi.", ex);
            MessageBox.Show("Ayarlar kaydedilirken bir hata oluştu.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BrowseSaveFolder()
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Varsayılan kayıt klasörünü seçin.",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(SaveFolder)
                ? SaveFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            SaveFolder = dialog.SelectedPath;
        }
    }

    private async void FetchUpdateFromInternet()
    {
        try
        {
            ManifestVersion = "Kontrol ediliyor...";
            ManifestMinVersion = "-";
            ManifestNotes = "Lütfen bekleyiniz...";
            ManifestDownloadUrl = "-";
            ManifestSha256 = "-";

            var manifest = await _services.UpdatePackageService.FetchLatestFromUrlAsync(_session.Settings.UpdateFeedUrl);
            ApplyManifest(manifest);

            if (!_services.UpdatePackageService.IsNewerVersion(manifest.Version, AppVersionInfo.CurrentVersion))
            {
                MessageBox.Show("Güncelleştirme yok. En yeni sürümü kullanıyorsunuz.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                throw new InvalidDataException("latest.json içinde indirme bağlantısı bulunamadı.");
            }

            if (!ConfirmUpdate(manifest))
            {
                return;
            }

            var packagePath = await _services.UpdatePackageService.DownloadPackageAsync(manifest);
            _services.UpdatePackageService.ValidatePackageForInstall(packagePath, AppVersionInfo.CurrentVersion, manifest.Sha256);
            LastUpdatePackagePath = packagePath;

            StartUpdater(packagePath, manifest.Sha256);
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("İnternetten güncelleme alınamadı.", ex);
            ManifestVersion = "Hata";
            ManifestMinVersion = "-";
            ManifestNotes = ex.Message;
            MessageBox.Show($"Güncelleme başlatılamadı:\n{ex.Message}", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadUpdateFromFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Güncelleme paketi seç",
            Filter = "ZIP Paketleri (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var manifest = _services.UpdatePackageService.ReadManifestFromZip(dialog.FileName);
            _services.UpdatePackageService.ValidatePackageForInstall(dialog.FileName, AppVersionInfo.CurrentVersion);

            LastUpdatePackagePath = dialog.FileName;
            ApplyManifest(manifest);

            if (!ConfirmUpdate(manifest))
            {
                MessageBox.Show("Paket geçerli, güncelleme iptal edildi.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StartUpdater(dialog.FileName, null);
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Güncelleme paketi seçilirken hata oluştu.", ex);
            MessageBox.Show($"Seçilen güncelleme paketi kullanılamadı:\n{ex.Message}", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearCanvas()
    {
        var result = MessageBox.Show(
            "Tüm çizimleri temizlemek istediğinize emin misiniz?",
            "++PEN",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _session.ClearAll();
        }
    }

    private void ResetSettings()
    {
        var result = MessageBox.Show(
            "Tüm ayarları varsayılan hale döndürmek istiyor musunuz?",
            "++PEN",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var defaults = new AppSettingsModel();
        _services.AppSettingsService.Save(defaults);
        _session.ApplySettings(defaults);
        _services.AppThemeService.Apply(defaults);
        _services.AutoSaveService.Apply(defaults);
        if (Application.Current.MainWindow is Views.ToolbarWindow toolbar)
        {
            toolbar.ApplySettings(defaults);
            _services.OverlayWindowService.ApplySettings(toolbar);
        }

        MessageBox.Show("Ayarlar sıfırlandı. Pencereyi yeniden açarak güncel değerleri görebilirsiniz.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearCache()
    {
        try
        {
            var cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlusPlusPen", "cache");
            Directory.CreateDirectory(cacheFolder);
            foreach (var file in Directory.GetFiles(cacheFolder))
            {
                File.Delete(file);
            }

            MessageBox.Show("Önbellek temizlendi.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Önbellek temizlenemedi.", ex);
            MessageBox.Show("Önbellek temizlenirken bir hata oluştu.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenLog()
    {
        _services.LogService.OpenLog();
    }

    private void ClearLogs()
    {
        _services.LogService.Clear();
        MessageBox.Show("Loglar temizlendi.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void ShowUninstallInfo()
    {
        MessageBox.Show(
            "++PEN’i kaldırmak için Windows Ayarları > Uygulamalar bölümünü kullanın.",
            "++PEN",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ApplyManifest(UpdateManifestModel manifest)
    {
        ManifestVersion = UpdatePackageService.FormatDisplayVersion(manifest.Version);
        ManifestMinVersion = UpdatePackageService.FormatDisplayVersion(manifest.MinVersion);
        ManifestNotes = manifest.Notes;
        ManifestDownloadUrl = string.IsNullOrWhiteSpace(manifest.DownloadUrl) ? "-" : manifest.DownloadUrl;
        ManifestSha256 = string.IsNullOrWhiteSpace(manifest.Sha256) ? "-" : manifest.Sha256;
    }

    private bool ConfirmUpdate(UpdateManifestModel manifest)
    {
        var message = "++PEN güncellenecek ve yeniden başlatılacak. Devam edilsin?";
        if (_session.BackgroundSnapshot is not null || _session.Strokes.Count > 0)
        {
            message += "\n\nKaydedilmemiş çizimler olabilir.";
        }

        message += $"\n\nYeni sürüm: {UpdatePackageService.FormatDisplayVersion(manifest.Version)}";
        return MessageBox.Show(message, "++PEN Güncelleştirme", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private void StartUpdater(string packagePath, string? expectedSha256)
    {
        try
        {
            var installDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var appExePath = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, "PlusPlusPen.exe");

            _services.UpdaterLaunchService.Launch(
                packagePath,
                installDirectory,
                appExePath,
                Environment.ProcessId,
                AppVersionInfo.CurrentVersion,
                expectedSha256);

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Güncelleştirme Merkezi başlatılamadı.", ex);
            MessageBox.Show($"Güncelleştirme Merkezi başlatılamadı:\n{ex.Message}", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private AppSettingsModel BuildSettingsModel()
    {
        var minThickness = Math.Min(MinimumStrokeThickness, MaximumStrokeThickness);
        var maxThickness = Math.Max(MinimumStrokeThickness, MaximumStrokeThickness);
        var smoothingEnabled = SmoothingPreset != SmoothingPresetOption.Off;
        var smoothingLevel = SmoothingPreset switch
        {
            SmoothingPresetOption.Low => 0.28,
            SmoothingPresetOption.High => 0.9,
            SmoothingPresetOption.Medium => 0.62,
            _ => 0.05
        };
        var liveSmoothingEnabled = SmoothingPreset is SmoothingPresetOption.Medium or SmoothingPresetOption.High;
        var fountainEffectEnabled = PenStyle is PenStyleOption.FountainPen or PenStyleOption.FeltTip;

        return new AppSettingsModel
        {
            LaunchAtStartup = LaunchAtStartup,
            KeepPanelOnTop = KeepPanelOnTop,
            PanelOpacity = Math.Clamp(PanelOpacityPercent / 100d, 0.2, 1.0),
            PanelSize = PanelSize,
            DefaultTool = DefaultTool,
            Language = Language,
            Theme = SelectedTheme,
            AccentColorHex = AccentColorHex,
            ActiveToolColorHex = ActiveToolColorHex,
            DefaultThickness = DefaultThickness,
            PenStyle = PenStyle,
            PenSensitivity = PenSensitivity,
            SmoothingPreset = SmoothingPreset,
            DynamicThicknessEnabled = DynamicThicknessEnabled,
            VelocityBasedThicknessEnabled = VelocityBasedThicknessEnabled,
            StrokeTaperEnabled = StrokeTaperEnabled,
            AutoStraightLineEnabled = AutoStraightLineEnabled,
            StraightLineSensitivity = Math.Clamp(StraightLineSensitivity / 100d, 0.5, 0.98),
            FountainPenEffectEnabled = fountainEffectEnabled,
            PerformanceModeEnabled = PerformanceModeEnabled,
            LiveSmoothingEnabled = liveSmoothingEnabled,
            SmoothingEnabled = smoothingEnabled,
            SmoothingLevel = Math.Clamp(smoothingLevel, 0.05, 1.0),
            MinimumPointDistance = Math.Max(0.0, MinimumPointDistance),
            InterpolationLimit = Math.Max(1, InterpolationLimit),
            MinimumStrokeThickness = minThickness,
            MaximumStrokeThickness = maxThickness,
            MouseSpeedAffectsThickness = MouseSpeedAffectsThickness && VelocityBasedThicknessEnabled,
            StylusPressureEnabled = StylusPressureEnabled,
            PalmRejectionPlanned = PalmRejectionPlanned,
            EraserSize = EraserSize,
            EraserMode = EraserMode,
            SaveFolder = SaveFolder,
            SaveAsPng = true,
            TransparentBackgroundEnabled = TransparentBackgroundEnabled,
            FileNamePattern = FileNamePattern,
            AutoSaveEnabled = AutoSaveEnabled,
            AutoSaveInterval = AutoSaveInterval,
            LastUpdatePackagePath = LastUpdatePackagePath,
            UpdateFeedUrl = _session.Settings.UpdateFeedUrl
        };
    }

    private void CloseWindow()
    {
        if (Application.Current.Windows.OfType<Window>().SingleOrDefault(x => ReferenceEquals(x.DataContext, this)) is Window window)
        {
            window.Close();
        }
    }
}
