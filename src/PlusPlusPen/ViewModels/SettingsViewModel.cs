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
    private const string CurrentVersionValue = "v0.1.0";
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
    private PenSensitivity _penSensitivity;
    private bool _dynamicThicknessEnabled;
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
        _penSensitivity = settings.PenSensitivity;
        _dynamicThicknessEnabled = settings.DynamicThicknessEnabled;
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
        _manifestNotes = "Bu özellik tam güncelleme motoru sonraki sürümde aktif olacak.";

        ThemeOptions = new ObservableCollection<ThemeMode>(Enum.GetValues<ThemeMode>());
        PanelSizeOptions = new ObservableCollection<PanelSizeOption>(Enum.GetValues<PanelSizeOption>());
        ToolOptions = new ObservableCollection<ToolKind>(Enum.GetValues<ToolKind>());
        LanguageOptions = new ObservableCollection<AppLanguage>(Enum.GetValues<AppLanguage>());
        PenSensitivityOptions = new ObservableCollection<PenSensitivity>(Enum.GetValues<PenSensitivity>());
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

    public ObservableCollection<PenSensitivity> PenSensitivityOptions { get; }

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

    public PenSensitivity PenSensitivity
    {
        get => _penSensitivity;
        set => SetProperty(ref _penSensitivity, value);
    }

    public bool DynamicThicknessEnabled
    {
        get => _dynamicThicknessEnabled;
        set => SetProperty(ref _dynamicThicknessEnabled, value);
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

    public string CurrentVersion => CurrentVersionValue;

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

    public string LastUpdatePackagePath
    {
        get => _lastUpdatePackagePath;
        set => SetProperty(ref _lastUpdatePackagePath, value);
    }

    public string FileNamePattern => "pluspluspen_YYYYMMDD_HHMMSS";

    public string PalmRejectionStatus => "Yakında";

    public string UpdateFeatureNote => "Bu özellik tam güncelleme motoru sonraki sürümde aktif olacak.";

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

            var settings = _session.Settings;
            var manifest = await _services.UpdatePackageService.FetchLatestFromUrlAsync(settings.UpdateFeedUrl);

            if (string.IsNullOrWhiteSpace(manifest.Version))
            {
                MessageBox.Show("Sürüm bilgisi alınamadı.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ManifestVersion = manifest.Version;
            ManifestMinVersion = manifest.MinVersion;
            ManifestNotes = manifest.Notes;

            // Mevcut versiyon ile karşılaştır
            if (IsNewerVersion(manifest.Version, CurrentVersion))
            {
                MessageBox.Show($"Yeni sürüm bulundu: {manifest.Version}", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Güncelleştirme yok - halihazırda en yeni sürümü kullanıyorsunuz.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("İnternetten güncelleme kontrol edilirken hata oluştu.", ex);
            ManifestVersion = "Hata: Bağlantı başarısız";
            ManifestMinVersion = "-";
            ManifestNotes = $"Hata: {ex.Message}";
            MessageBox.Show("İnternetten güncelleme bilgisi alınamadı.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        try
        {
            var newVer = newVersion.TrimStart('v');
            var curVer = currentVersion.TrimStart('v');

            var newParts = newVer.Split('.').Select(p => int.TryParse(p, out var i) ? i : 0).ToArray();
            var curParts = curVer.Split('.').Select(p => int.TryParse(p, out var i) ? i : 0).ToArray();

            for (int i = 0; i < Math.Max(newParts.Length, curParts.Length); i++)
            {
                var n = i < newParts.Length ? newParts[i] : 0;
                var c = i < curParts.Length ? curParts[i] : 0;

                if (n > c) return true;
                if (n < c) return false;
            }

            return false;
        }
        catch
        {
            return false;
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
            LastUpdatePackagePath = dialog.FileName;
            ManifestVersion = manifest.Version;
            ManifestMinVersion = manifest.MinVersion;
            ManifestNotes = manifest.Notes;
        }
        catch (Exception ex)
        {
            _services.LogService.LogError("Güncelleme paketi seçilirken hata oluştu.", ex);
            MessageBox.Show("Seçilen güncelleme paketi geçersiz veya bozuk.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private AppSettingsModel BuildSettingsModel()
    {
        var minThickness = Math.Min(MinimumStrokeThickness, MaximumStrokeThickness);
        var maxThickness = Math.Max(MinimumStrokeThickness, MaximumStrokeThickness);

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
            PenSensitivity = PenSensitivity,
            DynamicThicknessEnabled = DynamicThicknessEnabled,
            FountainPenEffectEnabled = FountainPenEffectEnabled,
            PerformanceModeEnabled = PerformanceModeEnabled,
            LiveSmoothingEnabled = LiveSmoothingEnabled,
            SmoothingEnabled = SmoothingEnabled,
            SmoothingLevel = Math.Clamp(SmoothingLevel / 100d, 0.05, 1.0),
            MinimumPointDistance = Math.Max(0.0, MinimumPointDistance),
            InterpolationLimit = Math.Max(1, InterpolationLimit),
            MinimumStrokeThickness = minThickness,
            MaximumStrokeThickness = maxThickness,
            MouseSpeedAffectsThickness = MouseSpeedAffectsThickness,
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
            LastUpdatePackagePath = LastUpdatePackagePath
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
