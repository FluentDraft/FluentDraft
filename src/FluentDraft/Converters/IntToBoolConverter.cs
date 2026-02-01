using System;
using System.Globalization;
using System.Windows.Data;

namespace FluentDraft.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && int.TryParse(parameter?.ToString(), out int targetInt))
            {
                return intValue == targetInt;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && int.TryParse(parameter?.ToString(), out int targetInt))
            {
                return targetInt;
            }
            return Binding.DoNothing;
        }
    }
}
