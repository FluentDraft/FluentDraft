using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace FluentDraft.Views
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            LoadApp();
        }

        private async void LoadApp()
        {
            // Emulate loading
            await Task.Delay(3000);

            // Fade out animation
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };

            fadeOut.Completed += (s, e) => Close();

            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
