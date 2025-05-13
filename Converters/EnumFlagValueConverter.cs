using System.Globalization;
using System.Windows.Data;

namespace Mapper_v1.Converters
{
    public class EnumFlagValueConverter : IValueConverter
    {
        private int targetValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int mask = (int)parameter;
            targetValue = (int)value;
            return (targetValue & mask) != 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            targetValue ^= (int)parameter;
            return Enum.Parse(targetType, targetValue.ToString());
        }
    }
}
