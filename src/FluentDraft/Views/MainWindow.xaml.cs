using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using FluentDraft.ViewModels;

namespace FluentDraft.Views
{
    public partial class MainWindow : Window
    {
        private bool _isExiting = false;

        private SettingsWindow? _settingsWindow;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = viewModel;
            _serviceProvider = serviceProvider;
            PreviewKeyDown += MainWindow_KeyDown;

            // Subscribe to ViewModel property changes
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsSettingsVisible))
            {
                var vm = DataContext as MainViewModel;
                if (vm?.IsSettingsVisible == true)
                {
                    OpenSettings();
                }
                else
                {
                    _settingsWindow?.Hide();
                }
            }
        }

        private void OpenSettings()
        {
            if (_settingsWindow == null)
            {
                // Use DI to resolve SettingsWindow (and its ViewModel)
                _settingsWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<SettingsWindow>(_serviceProvider);
                _settingsWindow.Owner = this;
                _settingsWindow.Closed += (s, args) =>
                {
                    _settingsWindow = null;
                    if (DataContext is MainViewModel mainVm) mainVm.IsSettingsVisible = false;
                };
            }

            _settingsWindow?.Show();
            _settingsWindow?.Activate();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Hotkey recording moved to SettingsViewModel/SettingsWindow
                if (e.Key == Key.Escape)
                {
                    if (vm.CancelCommand.CanExecute(null))
                    {
                        vm.CancelCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExiting)
            {
                var vm = DataContext as MainViewModel;
                if (vm != null && vm.CloseToTray)
                {
                    e.Cancel = true;
                    Hide();
                }
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            TrayIcon?.Dispose();
            base.OnClosed(e);
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowAndActivate();
        }
        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowAndActivate();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _isExiting = true;
            Close();
            Application.Current.Shutdown();
        }

        private void ShowAndActivate()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
