using System.Windows;
using System.Windows.Media;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class AppThemeService
{
    public void Apply(AppSettingsModel settings)
    {
        var resources = Application.Current.Resources;
        var palette = BuildPalette(settings);

        resources["PanelBlueColor"] = palette.PanelBlue;
        resources["PanelBorderColor"] = palette.PanelBorder;
        resources["PanelAccentColor"] = palette.Accent;
        resources["PanelActiveGreenColor"] = palette.ActiveTool;
        resources["PanelTextColor"] = palette.Text;
        resources["PanelBlueBrush"] = new SolidColorBrush(palette.PanelBlue);
        resources["PanelBorderBrush"] = new SolidColorBrush(palette.PanelBorder);
        resources["PanelAccentBrush"] = new SolidColorBrush(palette.Accent);
        resources["PanelActiveGreenBrush"] = new SolidColorBrush(palette.ActiveTool);
        resources["PanelTextBrush"] = new SolidColorBrush(palette.Text);
    }

    private static ThemePalette BuildPalette(AppSettingsModel settings)
    {
        var accent = AppSettingsModel.ParseColor(settings.AccentColorHex, "#FF57C1FF");
        var active = AppSettingsModel.ParseColor(settings.ActiveToolColorHex, "#FF3CBD5B");

        return settings.Theme switch
        {
            ThemeMode.DarkBlue => new ThemePalette(
                Color.FromArgb((byte)Math.Round(settings.PanelOpacity * 255), 14, 27, 52),
                Color.FromRgb(71, 143, 255),
                accent,
                active,
                Color.FromRgb(232, 244, 255)),
            ThemeMode.Black => new ThemePalette(
                Color.FromArgb((byte)Math.Round(settings.PanelOpacity * 255), 17, 17, 20),
                Color.FromRgb(100, 100, 108),
                accent,
                active,
                Color.FromRgb(244, 244, 246)),
            _ => new ThemePalette(
                Color.FromArgb((byte)Math.Round(settings.PanelOpacity * 255), 219, 244, 255),
                Color.FromRgb(58, 166, 232),
                accent,
                active,
                Color.FromRgb(12, 42, 79))
        };
    }

    private sealed record ThemePalette(
        Color PanelBlue,
        Color PanelBorder,
        Color Accent,
        Color ActiveTool,
        Color Text);
}
