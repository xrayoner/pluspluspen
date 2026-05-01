using System.Windows.Media;

namespace PlusPlusPen.Models;

public sealed class AppSettingsModel
{
    public bool LaunchAtStartup { get; set; }

    public bool KeepPanelOnTop { get; set; } = true;

    public double PanelOpacity { get; set; } = 0.92;

    public PanelSizeOption PanelSize { get; set; } = PanelSizeOption.Medium;

    public ToolKind DefaultTool { get; set; } = ToolKind.Pen;

    public AppLanguage Language { get; set; } = AppLanguage.Turkish;

    public ThemeMode Theme { get; set; } = ThemeMode.LightBlue;

    public string AccentColorHex { get; set; } = "#FF57C1FF";

    public string ActiveToolColorHex { get; set; } = "#FF3CBD5B";

    public double DefaultThickness { get; set; } = 7;

    public PenSensitivity PenSensitivity { get; set; } = PenSensitivity.Medium;

    public bool DynamicThicknessEnabled { get; set; } = true;

    public bool FountainPenEffectEnabled { get; set; } = true;

    public bool SmoothingEnabled { get; set; } = true;

    public double SmoothingLevel { get; set; } = 0.55;

    public double MinimumStrokeThickness { get; set; } = 2;

    public double MaximumStrokeThickness { get; set; } = 16;

    public bool MouseSpeedAffectsThickness { get; set; } = true;

    public bool StylusPressureEnabled { get; set; } = true;

    public bool PalmRejectionPlanned { get; set; } = true;

    public double EraserSize { get; set; } = 18;

    public EraserMode EraserMode { get; set; } = EraserMode.PartialStroke;

    public string SaveFolder { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public bool SaveAsPng { get; set; } = true;

    public bool TransparentBackgroundEnabled { get; set; } = true;

    public string FileNamePattern { get; set; } = "pluspluspen_YYYYMMDD_HHMMSS";

    public bool AutoSaveEnabled { get; set; }

    public AutoSaveIntervalOption AutoSaveInterval { get; set; } = AutoSaveIntervalOption.FiveMinutes;

    public string LastUpdatePackagePath { get; set; } = string.Empty;

    public static Color ParseColor(string colorHex, string fallbackHex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(colorHex);
        }
        catch
        {
            return (Color)ColorConverter.ConvertFromString(fallbackHex);
        }
    }
}
