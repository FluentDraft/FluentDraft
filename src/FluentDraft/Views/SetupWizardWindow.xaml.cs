using System.Windows;
using System.Windows.Input;
using FluentDraft.ViewModels;

namespace FluentDraft.Views
{
    public partial class SetupWizardWindow : Window
    {
        public SetupWizardWindow(SetupWizardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.OnRequestClose += () => 
            {
                DialogResult = true;
                Close();
            };
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is SetupWizardViewModel vm && vm.IsRecordingHotkey)
            {
                vm.HandleHotkeyInput(e.Key);
                e.Handled = true;
            }
        }
    }
}
