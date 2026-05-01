using System.Globalization;
using System.Windows.Data;
using PlusPlusPen.Models;

namespace PlusPlusPen.ViewModels;

public sealed class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ThemeMode.LightBlue => "Açık mavi tema",
            ThemeMode.DarkBlue => "Koyu mavi tema",
            ThemeMode.Black => "Siyah tema",
            PanelSizeOption.Small => "Küçük",
            PanelSizeOption.Medium => "Orta",
            PanelSizeOption.Large => "Büyük",
            ToolKind.Pen => "Kalem",
            ToolKind.Eraser => "Silgi",
            AppLanguage.Turkish => "Türkçe",
            AppLanguage.English => "İngilizce",
            PenSensitivity.Low => "Düşük",
            PenSensitivity.Medium => "Orta",
            PenSensitivity.High => "Yüksek",
            EraserMode.PartialStroke => "Çizgi parçası silme modu",
            EraserMode.WholeStroke => "Tüm çizgiyi silme modu",
            AutoSaveIntervalOption.OneMinute => "1 dk",
            AutoSaveIntervalOption.FiveMinutes => "5 dk",
            AutoSaveIntervalOption.TenMinutes => "10 dk",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
