using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace FluentDraft.Converters
{
    public class StatusColorConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "";
            
            if (status.StartsWith("Recording")) return Brushes.Red;
            if (status.StartsWith("Transcribing")) return Brushes.Yellow;
            if (status.StartsWith("Typing")) return Brushes.LightBlue;
            if (status.StartsWith("Error")) return Brushes.OrangeRed;
            if (status == "Ready") return Brushes.LimeGreen;

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
