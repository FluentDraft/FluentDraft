using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FluentDraft.Converters
{
    public class RecordingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? param = parameter as string;

            // Window Resizing Logic
            if (param == "SizeToContent")
            {
                // Always return Height so it auto-expands when history opens, 
                // and snaps back when forced on close. 
                // User resizing will inherently switch it to Manual temporarily.
                return System.Windows.SizeToContent.Height;
            }

            if (param == "ResizeMode")
            {
                if (value is bool isHistoryVisible && isHistoryVisible)
                    return System.Windows.ResizeMode.CanResize;
                return System.Windows.ResizeMode.NoResize;
            }

            // Visibility Logic
            if (param != null && param.EndsWith("Vis"))
            {
                if (param == "HasTextVis")
                {
                    return !string.IsNullOrEmpty(value as string) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
                if (param == "NoTextVis")
                {
                    return string.IsNullOrEmpty(value as string) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }

                string state = value?.ToString() ?? "None";
                bool isMatch = false;

                if (param == "ListeningVis" && state == "Listening") isMatch = true;
                else if (param == "TranscribingVis" && state == "Transcribing") isMatch = true;
                else if (param == "DoneVis" && state == "Done") isMatch = true;
                else if (param == "NoneVis" && (state == "None" || string.IsNullOrEmpty(state))) isMatch = true;

                return isMatch ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }

            // Refinement State Logic
            if (param == "StateColor" || param == "StateBrush")
            {
                bool isEnabled = value is bool b && b;
                return isEnabled ? new SolidColorBrush(Color.FromRgb(0, 122, 255)) : new SolidColorBrush(Color.FromRgb(102, 102, 102)); // #007AFF vs #666
            }

            // Original Color Logic
            if (value is bool isRecording && isRecording)
            {
                return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
