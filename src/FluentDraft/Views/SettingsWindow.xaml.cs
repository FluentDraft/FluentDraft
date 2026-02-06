using System.Windows;
using FluentDraft.ViewModels;

namespace FluentDraft.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly System.IServiceProvider _serviceProvider;

        public SettingsWindow(SettingsViewModel viewModel, System.IServiceProvider serviceProvider)
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
                // Wizard completed
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && vm.IsRecordingHotkey)
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
