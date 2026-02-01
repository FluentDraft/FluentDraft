using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FluentDraft.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramStr)
            {
                bool inverse = false;
                if (paramStr.EndsWith("Inv"))
                {
                    inverse = true;
                    paramStr = paramStr.Substring(0, paramStr.Length - 3);
                }

                if (int.TryParse(paramStr, out int targetInt))
                {
                    bool matches = intValue == targetInt;
                    if (inverse) matches = !matches;

                    return matches ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
