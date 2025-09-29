using ControlzEx.Theming;
using Mapper_v1.Core;
using System.Windows;
using System.Windows.Media;

namespace Mapper_v1.Helpers;

public static class Formater
{
    public static string FormatLatLong(this double ll, DegreeFormat degreeFormat)
    {
        int d, m = 0;
        double mm, ss = 0;
        switch (degreeFormat)
        {
            case DegreeFormat.Deg:
                return ll.ToString("F8");
            case DegreeFormat.Min:
                d = (int)ll;
                mm = (ll - d) * 60;
                return $"{d}\"{mm.ToString("F6")}";
            case DegreeFormat.Sec:
                d = (int)ll;
                mm = (ll - d) * 60;
                m = (int)mm;
                ss = (mm - m) * 60;
                return $"{d}\"{m}'{ss.ToString("F4")}";
            default:
                return ll.ToString();
        }
    }
    public static Color GetThemeForegroundColor()
    {
        var theme = ThemeManager.Current.DetectTheme(Application.Current);
        SolidColorBrush foregroundBrush = Application.Current.TryFindResource("MahApps.Brushes.Text") as SolidColorBrush;
        Color? foregroundColor = foregroundBrush?.Color;
        return foregroundColor ?? Colors.Black;
    }
    public static Color GetThemeBackgroundColor()
    {
        var theme = ThemeManager.Current.DetectTheme(Application.Current);
        SolidColorBrush backgroundBrush = Application.Current.TryFindResource("MahApps.Brushes.Background") as SolidColorBrush;
        Color? backgroundColor = backgroundBrush?.Color;
        return backgroundColor ?? Colors.White;
    }

}
