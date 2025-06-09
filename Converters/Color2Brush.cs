using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mapper_v1.Converters
{
    public class Color2Brush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color color)
            {
                return new System.Windows.Media.SolidColorBrush(color);
            }
            else if (value is System.Drawing.Color drawingColor)
            {
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B));
            }
            else if (value is System.Windows.Media.SolidColorBrush brush)
            {
                return brush;
            }
            else if (value is string colorString && System.Windows.Media.ColorConverter.ConvertFromString(colorString) is System.Windows.Media.Color parsedColor)
            {
                return new System.Windows.Media.SolidColorBrush(parsedColor);
            }
            // If the value is not a recognized type, return null
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.SolidColorBrush brush)
            {
                return brush.Color;
            }
            else if (value is System.Windows.Media.Color color)
            {
                return color;
            }
            else if (value is System.Drawing.Color drawingColor)
            {
                return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            }
            else if (value is string colorString)
            {
                try
                {
                    return System.Windows.Media.ColorConverter.ConvertFromString(colorString);
                }
                catch
                {
                    // Handle invalid color string
                    return null;
                }
            }
            // If the value is not a recognized type, return null
            return null;
        }
    }
}
