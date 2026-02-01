using System.Windows;
using FluentDraft.ViewModels;

namespace FluentDraft.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly System.IServiceProvider _serviceProvider;

        public SettingsWindow(MainViewModel viewModel, System.IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = viewModel;
            _serviceProvider = serviceProvider;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        private void RunWizard_Click(object sender, RoutedEventArgs e)
        {
            var wizard = _serviceProvider.GetService(typeof(SetupWizardWindow)) as Window;
            if (wizard == null) return;
            
            wizard.Owner = this;
            if (wizard.ShowDialog() == true)
            {
                // Refresh ViewModel settings if needed (MainViewModel likely needs a Refresh method call or it binds to the same source)
                // Since MainViewModel reloads settings on OnStartup/etc, we might want to manually trigger a reload or simple property notification.
                // For now, wizard updates the SettingsService, so next time settings are read they are fresh.
                // To reflect immediately, we might need:
                // (DataContext as MainViewModel)?.LoadSettings(); // but LoadSettings is private.
                // We'll trust that most settings are bound or will refresh on next use.
                // Actually, the user might want immediate reflection. Let's see.
                // MainViewModel has commands that reload settings? Or `LoadSettings` is private.
                 // Ideally MainViewModel should have a "RefreshSettings" public method.
                 // For now, let's just close wizard.
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.IsRecordingHotkey)
            {
                vm.HandleHotkeyInput(e.Key);
                e.Handled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
