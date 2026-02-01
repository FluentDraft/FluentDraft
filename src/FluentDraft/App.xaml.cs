using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using FluentDraft.Services;
using FluentDraft.Services.Implementations;
using FluentDraft.Services.Interfaces;
using FluentDraft.Utils;
using FluentDraft.ViewModels;
using FluentDraft.Views;

namespace FluentDraft
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;
        private static Mutex? _mutex = null;
        private const string MutexName = "Global\\FluentDraft_Mutex";

        public App()
        {
            try 
            {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now}: App Constructor Started\n");
                ServiceCollection services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();
                File.AppendAllText("startup_log.txt", $"{DateTime.Now}: DI Built\n");
            }
            catch (Exception ex)
            {
                 File.AppendAllText("startup_log.txt", $"{DateTime.Now}: App Constructor Error: {ex}\n");
                 MessageBox.Show($"Startup Error: {ex.Message}");
                 Shutdown();
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Logging
            services.AddSingleton<ILoggingService, FileLogger>();

            // Settings
            services.AddSingleton<ISettingsService, JsonSettingsService>();

            // Services
            services.AddSingleton<AudioDeviceService>();
            services.AddSingleton<IAudioRecorder, NAudioRecorder>();
            services.AddSingleton<IInputInjector, WindowsInputInjector>();
            
            // Transcription Providers
            services.AddSingleton<ITranscriptionService, OpenAiCompatibleTranscriptionService>();

            // Text Processing
            services.AddSingleton<ITextProcessorService, UniversalTextProcessor>();
            services.AddSingleton<ISystemControlService, WindowsSystemControlService>();

            // Utils
            services.AddSingleton<GlobalHotkeyManager>();
            services.AddSingleton<IHistoryService, JSONHistoryService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SetupWizardViewModel>();

            // Windows
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<SetupWizardWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Single Instance Check
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                if (OperatingSystem.IsWindows())
                {
                    // App is already running! Bring it to front.
                    IntPtr hWnd = WindowsNative.FindWindow(null, "FluentDraft");
                    if (hWnd != IntPtr.Zero)
                    {
                        WindowsNative.ShowWindow(hWnd, WindowsNative.SW_RESTORE);
                        WindowsNative.SetForegroundWindow(hWnd);
                    }
                    else
                    {
                        MessageBox.Show("FluentDraft is already running in the background.", "Instance Active", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                
                // Release local mutex reference so we don't hold it (though process exit clears it anyway)
                _mutex.Dispose();
                _mutex = null;
                
                Shutdown();
                return;
            }

            base.OnStartup(e);
            File.AppendAllText("startup_log.txt", $"{DateTime.Now}: OnStartup Started\n");
            try 
            {
                var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();
                var settings = settingsService.LoadSettings();

                if (!settings.IsSetupCompleted)
                {
                    File.AppendAllText("startup_log.txt", $"{DateTime.Now}: Launching Setup Wizard\n");
                    var wizard = ServiceProvider.GetRequiredService<SetupWizardWindow>();
                    if (wizard.ShowDialog() == true)
                    {
                        File.AppendAllText("startup_log.txt", $"{DateTime.Now}: Wizard Completed. Launching Main\n");
                        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                        Application.Current.MainWindow = mainWindow;
                        mainWindow.Show();
                        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    }
                    else
                    {
                        File.AppendAllText("startup_log.txt", $"{DateTime.Now}: Wizard Cancelled. Shutdown.\n");
                        Shutdown();
                    }
                }
                else
                {
                    File.AppendAllText("startup_log.txt", $"{DateTime.Now}: Launching MainWindow\n");
                    var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now}: OnStartup Error: {ex}\n");
                MessageBox.Show($"Startup Error: {ex}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_mutex != null)
                {
                    // If we own the mutex, release it
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }

                if (ServiceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
            }
            finally
            {
                base.OnExit(e);
                // Aggressive Shutdown to ensure no background threads remain
                Environment.Exit(0);
            }
        }

        private static class WindowsNative
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

            public const int SW_RESTORE = 9;
        }
    }
}
