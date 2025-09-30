using System;
using System.Globalization;
using System.Windows.Data;

namespace Mapper_v1.Converters
{
    public class DoubleFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                int decimalPlaces = 2;
                if (parameter is int dp)
                    decimalPlaces = dp;
                else if (parameter is string s && int.TryParse(s, out int parsed))
                    decimalPlaces = parsed;
                return d.ToString($"F{decimalPlaces}", culture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(double) && value is string s && double.TryParse(s, out double d))
                return d;
            return value;
        }
    }
}
