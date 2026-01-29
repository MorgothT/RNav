using System.Runtime.CompilerServices;
using System.Windows.Media;
using Color = Mapsui.Styles.Color;

namespace Mapper_v1.Converters;

public static class ColorConvertor
{
    public static Color WMColorToMapsui(System.Windows.Media.Color from)
    {
        return new Color(from.R, from.G, from.B, from.A);
    }
    public static Color ToMapsuiColor(this System.Windows.Media.Color from)
    {
        return new Color(from.R, from.G, from.B, from.A);
    }

    public static System.Windows.Media.Color WMColorFromMapsui(Color from)
    {
        System.Windows.Media.Color color;
        color.R = (byte)from.R;
        color.G = (byte)from.G;
        color.B = (byte)from.B;
        color.A = (byte)from.A;
        return color;
    }
    public static System.Windows.Media.Color ToWMColor(this Color from)
    {
        System.Windows.Media.Color color;
        color.R = (byte)from.R;
        color.G = (byte)from.G;
        color.B = (byte)from.B;
        color.A = (byte)from.A;
        return color;
    }
    public static Color String2Mapsui(string from)
    {
        try
        {
            int[] arr = from.Split(',').Select(x => int.Parse(x)).ToArray();
            return new Color(arr[0], arr[1], arr[2], arr[3]);
        }
        catch (Exception)
        {
            return Color.FromArgb(255, 180, 200, 220); //default
        }
    }
    public static Color ToMapsuiColor(this string from)
    {
        try
        {
            int[] arr = from.Split(',').Select(x => int.Parse(x)).ToArray();
            return new Color(arr[0], arr[1], arr[2], arr[3]);
        }
        catch (Exception)
        {
            return Color.FromArgb(255, 180, 200, 220); //default
        }
    }
    public static string Mapsui2String(Color from)
    {
        return $"{from.R},{from.G},{from.B},{from.A}";
    }
    public static string ToColorString(this Color from)
    {
        return $"{from.R},{from.G},{from.B},{from.A}";
    }
    public static Color InvertColor(this Color color)
    {
        return new Color((byte)~color.R, (byte)~color.G, (byte)~color.B, color.A);
        //var wmcolor = WMColorFromMapsui(color);
        //System.Windows.Media.Color inverted = System.Windows.Media.Color.FromRgb((byte)~wmcolor.R, (byte)~wmcolor.G, (byte)~wmcolor.B);
        //return WMColorToMapsui(inverted);
    }
    //public static System.Windows.Media.Color InvertColor(System.Windows.Media.Color color)
    //{
    //    return System.Windows.Media.Color.FromRgb((byte)~color.R, (byte)~color.G, (byte)~color.B);
    //}
    public static System.Windows.Media.Color InvertColor(this System.Windows.Media.Color color)
    {
        return System.Windows.Media.Color.FromRgb((byte)~color.R, (byte)~color.G, (byte)~color.B);
    }
    public static System.Windows.Media.Brush InvertBrushColor(this Brush brush)
    {
        return new SolidColorBrush(((SolidColorBrush)brush).Color.InvertColor());
        //    var wmcolor = WMColorFromMapsui(color);
        //    System.Windows.Media.Color inverted = System.Windows.Media.Color.FromRgb((byte)~wmcolor.R, (byte)~wmcolor.G, (byte)~wmcolor.B);
        //    return new SolidColorBrush(inverted);
        //}
    }
    public static System.Windows.Media.Brush ToBrush(this Color color)
    {
        return new SolidColorBrush(color.ToWMColor());
    }
}
