using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentDraft.Messages;
using FluentDraft.Models;
using FluentDraft.Services;
using FluentDraft.Services.Interfaces;
using FluentDraft.Utils;
using Velopack;

namespace FluentDraft.ViewModels
{
    public class AudioDeviceModel
    {
        public int DeviceNumber { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public override string ToString() => ProductName;
    }

    public partial class WaveBar : ObservableObject
    {
        [ObservableProperty]
        private double _height = 4.0;
    }

    public partial class MainViewModel : ObservableObject, IRecipient<SettingsChangedMessage>, IRecipient<UpdateAvailableMessage>, IRecipient<UpdateReadyMessage>, IRecipient<UpdateProgressMessage>
    {
        private readonly IAudioRecorder _audioRecorder;
        private readonly IInputInjector _inputInjector;
        private readonly ITranscriptionService _transcriptionService;
        private readonly ITextProcessorService _textProcessor;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private readonly ILoggingService _logger;
        private readonly ISettingsService _settingsService;

        private readonly ISystemControlService _systemControl;

        private readonly AudioDeviceService _audioDeviceService;
        private readonly IHistoryService _historyService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _status = "Ready";

        // Providers (Read-only view for operation)
        [ObservableProperty]
        private ObservableCollection<ProviderProfile> _providers = new();

        [ObservableProperty]
        private ProviderProfile? _selectedTranscriptionProfile;

        [ObservableProperty]
        private ProviderProfile? _selectedRefinementProfile;

        // Refinement Presets (Read-only view for operation)
        [ObservableProperty]
        private ObservableCollection<RefinementPreset> _refinementPresets = new();

        [ObservableProperty]
        private RefinementPreset? _selectedRefinementPreset;

        public ObservableCollection<ProviderProfile> AvailableTranscriptionProfiles => 
            new ObservableCollection<ProviderProfile>(Providers.Where(p => p.IsTranscriptionEnabled));

        public ObservableCollection<ProviderProfile> AvailableRefinementProfiles => 
            new ObservableCollection<ProviderProfile>(Providers.Where(p => p.IsRefinementEnabled));
        
        [ObservableProperty]
        private string _logs = "";

        [ObservableProperty]
        private bool _isAlwaysOnTop = false;

        [ObservableProperty]
        private bool _isPostProcessingEnabled = true;

        [ObservableProperty]
        private string _postProcessingPrompt = "";

        [ObservableProperty]
        private bool _pauseMediaOnRecording = false;

        [ObservableProperty]
        private string _hotkeyDisplay = "CapsLock";

        [ObservableProperty]
        private bool _isRecordingHotkey = false;

        [ObservableProperty]
        private bool _isSettingsVisible = false;

        [ObservableProperty]
        private bool _isAdvancedSettingsEnabled = false;

        [ObservableProperty]
        private ObservableCollection<AudioDeviceModel> _audioDevices = new();

        [ObservableProperty]
        private int _selectedAudioDevice = 0;

        [ObservableProperty]
        private string _lastTranscription = "";

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private int _activationMode = 1; // 0 = TapToTalk, 1 = PushToTalk

        [ObservableProperty]
        private bool _isHistoryVisible = false;

        [ObservableProperty]
        private bool _hasSelectedItems = false; 

        [ObservableProperty]
        private ObservableCollection<TranscriptionItem> _historyItems = new();
        [ObservableProperty]
        private int _textInjectionMode = 1; // 0 = Type, 1 = Paste

        [ObservableProperty]
        private double _volumeLevel = 0;

        [ObservableProperty]
        private bool _playSoundOnRecord = true;

        [ObservableProperty]
        private bool _closeToTray = true;

        [ObservableProperty]
        private int _maxRecordingSeconds = 120;

        [ObservableProperty]
        private string _instructionText = "Loading...";

        [ObservableProperty]
        private bool _isAutoInsertEnabled = true;

        private IntPtr _targetWindowHandle = IntPtr.Zero;

        private string _chatSessionId = ""; // Current User/Session ID

        public RelayCommand ResetChatSessionCommand { get; }

        private void UpdateInstructionText()
        {
             if (ActivationMode == 0) // Tap to Talk
             {
                 InstructionText = $"Press {HotkeyDisplay} to start/stop dictation";
             }
             else // Push to Talk
             {
                 InstructionText = $"Hold {HotkeyDisplay} to dictate";
             }
        }

        [ObservableProperty]
        private string _recordingTimeDisplay = "0,0s";

        [ObservableProperty]
        private string _processingTimeDisplay = "";

        [ObservableProperty]
        private string _uiState = "None"; // None, Listening, Transcribing, Done

        [ObservableProperty]
        private ObservableCollection<WaveBar> _audioWaves = new();

        [ObservableProperty]
        private string _trayIconSource = "/Icons/app_icon.ico";

        partial void OnUiStateChanged(string value)
        {
            if (value == "Listening")
            {
                TrayIconSource = "/Icons/recording_icon.ico";
            }
            else
            {
                TrayIconSource = "/Icons/app_icon.ico";
            }
        }


        private System.Windows.Threading.DispatcherTimer? _recordingTimer;
        private DateTime _recordingStartTime;
        private DateTime _processingStartTime;
        private bool _mediaWasPausedByUs = false;

        // About properties
        public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        public string DotNetVersion => Environment.Version.ToString();
        public string OsVersion => $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";

        private List<int> _currentHotkeyCodes = new List<int> { 0x14 };
        private CancellationTokenSource? _processingCts;


        public RelayCommand ToggleSettingsCommand { get; }
        public RelayCommand CloseSettingsCommand { get; }
        
        // Settings management moved to SettingsViewModel, 
        // but we still need to close the window if checking constraints?
        // Actually MainViewModel just toggles visibility usually.

        public RelayCommand CopyLastTranscriptionCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand StartManualRecordingCommand { get; }
        public RelayCommand SetTapToTalkCommand { get; }
        public RelayCommand SetPushToTalkCommand { get; }

        // History Commands
        public RelayCommand ToggleHistoryCommand { get; }
        public RelayCommand ClearHistoryCommand { get; }
        public RelayCommand<TranscriptionItem?> DeleteHistoryItemCommand { get; }
        public RelayCommand<TranscriptionItem?> CopyHistoryItemCommand { get; }
        public RelayCommand<TranscriptionItem?> PlayHistoryItemCommand { get; }
        public RelayCommand SelectAllHistoryCommand { get; }
        public RelayCommand DeselectAllHistoryCommand { get; }
        public RelayCommand CopySelectedHistoryCommand { get; }
        public RelayCommand<TranscriptionItem> ToggleHistoryItemExpandCommand { get; }
        
        public RelayCommand ManualInsertCommand { get; }
        public RelayCommand ProcessTextActionCommand { get; }


        public MainViewModel(
            IAudioRecorder audioRecorder,
            IInputInjector inputInjector,
            ITranscriptionService transcriptionService,
            ITextProcessorService textProcessor,
            GlobalHotkeyManager hotkeyManager,
            ILoggingService logger,
            ISettingsService settingsService,
            ISystemControlService systemControl,
            AudioDeviceService audioDeviceService,
            IHistoryService historyService,
            IUpdateService updateService,
            IServiceProvider serviceProvider)
        {
            _audioRecorder = audioRecorder;
            _inputInjector = inputInjector;
            _transcriptionService = transcriptionService;
            _textProcessor = textProcessor;
            _hotkeyManager = hotkeyManager;
            _logger = logger;
            _settingsService = settingsService;
            _systemControl = systemControl;
            _audioDeviceService = audioDeviceService;
            _historyService = historyService;
            _updateService = updateService;
            _serviceProvider = serviceProvider;

            // Register for messages
            WeakReferenceMessenger.Default.RegisterAll(this);

            LoadAudioDevices();

            // Initialize 60 bars for the wave animation
            for (int i = 0; i < 60; i++)
            {
                AudioWaves.Add(new WaveBar { Height = 3 }); 
            }


            ToggleSettingsCommand = new RelayCommand(OpenSettingsWindow);
            CloseSettingsCommand = new RelayCommand(RequestCloseSettings);
            CopyLastTranscriptionCommand = new RelayCommand(CopyLastTranscription);
            CancelCommand = new RelayCommand(CancelProcessing);
            StartManualRecordingCommand = new RelayCommand(ToggleRecordingManual);
            SetTapToTalkCommand = new RelayCommand(() => ActivationMode = 0);
            SetPushToTalkCommand = new RelayCommand(() => ActivationMode = 1);
            
            ToggleHistoryCommand = new RelayCommand(() => 
            {
                IsHistoryVisible = !IsHistoryVisible;
            });
            DeleteHistoryItemCommand = new RelayCommand<TranscriptionItem?>(async (item) => { if (item != null) await DeleteHistoryItem(item); });
            ClearHistoryCommand = new RelayCommand(async () => await ClearHistory());
            CopyHistoryItemCommand = new RelayCommand<TranscriptionItem?>(item => { if (item != null) CopyHistoryItem(item); });
            PlayHistoryItemCommand = new RelayCommand<TranscriptionItem?>(item => { if (item != null) PlayHistoryItem(item); });
            
            SelectAllHistoryCommand = new RelayCommand(SelectAllHistory);
            DeselectAllHistoryCommand = new RelayCommand(DeselectAllHistory);
            CopySelectedHistoryCommand = new RelayCommand(CopySelectedHistory);
            ToggleHistoryItemExpandCommand = new RelayCommand<TranscriptionItem>(item => 
            {
                if (item != null) item.IsExpanded = !item.IsExpanded;
            });

            ResetChatSessionCommand = new RelayCommand(ResetChatSession);

            UpdateNowCommand = new RelayCommand(ExecuteUpdateNow);
            DismissUpdateCommand = new RelayCommand(DismissUpdate);

            ManualInsertCommand = new RelayCommand(ExecuteManualInsert);
            ProcessTextActionCommand = new RelayCommand(() => 
            {
                if (IsAutoInsertEnabled) CopyLastTranscription();
                else ExecuteManualInsert();
            });

            _ = LoadSettingsAsync();
            InitializeHistory();

            _hotkeyManager.HotkeyDown += OnHotkeyDown;
            _hotkeyManager.Hotkeyup += OnHotkeyUp;

            _audioRecorder.VolumeChanged += OnVolumeChanged;

            _logger.OnLogAdded += message => Logs = _logger.GetLogContent();
            Logs = _logger.GetLogContent();

            _recordingTimer = new System.Windows.Threading.DispatcherTimer();
            _recordingTimer.Interval = TimeSpan.FromMilliseconds(100);
            _recordingTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _recordingStartTime;
                RecordingTimeDisplay = $"{elapsed.TotalSeconds:F1}s".Replace(".", ",");
                
                if (elapsed.TotalSeconds >= MaxRecordingSeconds)
                {
                    _logger.LogInfo($"Recording limit reached ({MaxRecordingSeconds}s). Auto-stopping.");
                    _ = StopAndProcessInternal(wasAutoStopped: true);
                }
            };

            _logger.LogInfo("MainViewModel initialized.");
        }

        public void Receive(SettingsChangedMessage message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                _ = LoadSettingsAsync();
            });
        }

        private void RequestCloseSettings()
        {
            // Close window logic
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is FluentDraft.Views.SettingsWindow)
                {
                    window.Close();
                    return;
                }
            }
        }

        private async void ToggleRecordingManual()
        {
            if (_audioRecorder.IsRecording) await StopAndProcessInternal();
            else OnHotkeyDown();
        }

        [ObservableProperty]
        private bool _isCopiedNotificationVisible;

        // Update Notification Properties
        [ObservableProperty]
        private bool _isUpdateNotificationVisible;

        [ObservableProperty]
        private string _updateNotificationTitle = "";

        [ObservableProperty]
        private string _updateNotificationButtonText = "Update";

        [ObservableProperty]
        private double _updateProgress = 0;

        [ObservableProperty]
        private bool _isUpdateDownloading = false;

        [ObservableProperty]
        private bool _isUpdateReadyToInstall = false;

        private UpdateInfo? _pendingUpdateInfo;
        private readonly IUpdateService? _updateService; // We need to inject this or resolve it

        public RelayCommand UpdateNowCommand { get; }
        public RelayCommand DismissUpdateCommand { get; }

        public void Receive(UpdateAvailableMessage message)
        {
             System.Windows.Application.Current.Dispatcher.Invoke(() => 
             {
                 _pendingUpdateInfo = message.Update;
                 UpdateNotificationTitle = $"New version {message.Update.TargetFullRelease.Version} available";
                 IsUpdateNotificationVisible = true;
                 IsUpdateDownloading = true; // Started download in background
                 UpdateProgress = 0;
                 UpdateNotificationButtonText = "Update"; // Will wait if clicked
                 IsUpdateReadyToInstall = false;
             });
        }

        public void Receive(UpdateProgressMessage message)
        {
             System.Windows.Application.Current.Dispatcher.Invoke(() => 
             {
                 UpdateProgress = message.Progress;
                 // If user hasn't dismissed, they see the bar filling
             });
        }

        public void Receive(UpdateReadyMessage message)
        {
             System.Windows.Application.Current.Dispatcher.Invoke(() => 
             {
                 _pendingUpdateInfo = message.Update;
                 IsUpdateDownloading = false;
                 IsUpdateReadyToInstall = true;
                 UpdateNotificationButtonText = "Restart";
                 UpdateProgress = 100;
                 
                 // If notification was dismissed (Later), we *could* show it again?
                 // Or just let Settings handle it.
                 // Let's bring it back if it was dismissed, or just update text if visible.
                 if (IsUpdateNotificationVisible)
                 {
                     UpdateNotificationTitle = $"Version {message.Update.TargetFullRelease.Version} ready to install";
                 }
             });
        }

        private void ExecuteUpdateNow()
        {
            if (_pendingUpdateInfo == null) return;

            if (IsUpdateReadyToInstall)
            {
                // Restart immediately
                _updateService?.ApplyUpdateAndRestart(_pendingUpdateInfo);
            }
            else
            {
                // User wants to update but it's still downloading.
                // We should probably just show a modal or keep the notification and disable the button?
                // Or since App.xaml.cs is downloading, we just wait.
                // Optimally: Change UI to invalid/waiting state.
                UpdateNotificationButtonText = "Downloading...";
                // We rely on UpdateReadyMessage to eventually enable Restart.
            }
        }

        private void DismissUpdate()
        {
            IsUpdateNotificationVisible = false;
        }

        private void CopyLastTranscription()
        {
            if (!string.IsNullOrEmpty(LastTranscription))
            {
                System.Windows.Clipboard.SetText(LastTranscription);
                _logger.LogInfo("Copied to clipboard.");
                
                IsCopiedNotificationVisible = true;
                Task.Delay(1500).ContinueWith(_ => IsCopiedNotificationVisible = false);
            }
        }

        private void CancelProcessing()
        {
            _recordingTimer?.Stop();
            
            if (_audioRecorder.IsRecording)
            {
                _audioRecorder.StopRecordingAsync();
                _logger.LogInfo("Recording cancelled by user.");
            }
            
            _processingCts?.Cancel();
            IsProcessing = false;
            UiState = "None";
            Status = "Cancelled";
            _ = Task.Delay(2000).ContinueWith(_ => 
            {
                if (UiState == "None") Status = "Ready";
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void ResetChatSession()
        {
            // if (System.Windows.MessageBox.Show("This will reset your AI Session ID.\nThe model will 'forget' previous context encoded in the user ID (if any).\nContinue?", "Reset Session", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
            // {
                _chatSessionId = Guid.NewGuid().ToString();
                
                var settings = await _settingsService.LoadSettingsAsync();
                settings.ChatSessionId = _chatSessionId;
                await _settingsService.SaveSettingsAsync(settings);
                
                _logger.LogInfo($"Chat Session ID reset to: {_chatSessionId}");
                Status = "Session Reset";
                _ = Task.Delay(2000).ContinueWith(_ => Status = "Ready");
            // }
        }

        private async Task LoadSettingsAsync()
        {
            if (_isLoadingSettings) return; // Prevent re-entry if already loading (though unlikely to be recursive here directly)
            _isLoadingSettings = true;
            try
            {
                var settings = await _settingsService.LoadSettingsAsync();
                
                if (settings.Providers != null)
                {
                    Providers = new ObservableCollection<ProviderProfile>(settings.Providers);
                    
                    SelectedTranscriptionProfile = Providers.FirstOrDefault(p => p.Id == settings.SelectedTranscriptionProfileId) ?? Providers.FirstOrDefault(p => p.IsTranscriptionEnabled);
                    SelectedRefinementProfile = Providers.FirstOrDefault(p => p.Id == settings.SelectedRefinementProfileId) ?? Providers.FirstOrDefault(p => p.IsRefinementEnabled);
                }

                if (settings.RefinementPresets != null)
                {
                    RefinementPresets = new ObservableCollection<RefinementPreset>(settings.RefinementPresets);
                    SelectedRefinementPreset = RefinementPresets.FirstOrDefault(p => p.Id == settings.SelectedRefinementPresetId) ?? RefinementPresets.FirstOrDefault();
                }

                IsAlwaysOnTop = settings.IsAlwaysOnTop;
                CloseToTray = settings.CloseToTray;
                IsPostProcessingEnabled = settings.IsPostProcessingEnabled;
                PostProcessingPrompt = settings.PostProcessingPrompt; 
                PlaySoundOnRecord = settings.PlaySoundOnRecord;
                PauseMediaOnRecording = settings.PauseMediaOnRecording;
                ActivationMode = settings.ActivationMode;
                TextInjectionMode = settings.TextInjectionMode;
                SelectedAudioDevice = settings.SelectedMicrophone;
                MaxRecordingSeconds = settings.MaxRecordingSeconds;
                _chatSessionId = settings.ChatSessionId ?? "";
                if (string.IsNullOrEmpty(_chatSessionId))
                {
                    // Should have been set by App.xaml.cs, but fallback just in case
                    _chatSessionId = Guid.NewGuid().ToString();
                }

                IsAutoInsertEnabled = settings.IsAutoInsertEnabled;

                _currentHotkeyCodes = settings.HotkeyCodes ?? new List<int> { 0x14 };
                UpdateHotkeyDisplay();
                _hotkeyManager.SetMonitoredKeys(_currentHotkeyCodes);
                
                RefreshFilteredCollections();
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }


        private bool _isLoadingSettings = false;

        public async Task SaveSettingsAsync()
        {
             if (_isLoadingSettings) return;

             var settings = await _settingsService.LoadSettingsAsync(); 
             
             if (Providers != null) settings.Providers = Providers.ToList(); 
             settings.SelectedTranscriptionProfileId = SelectedTranscriptionProfile?.Id;
             settings.SelectedRefinementProfileId = SelectedRefinementProfile?.Id;
             if (RefinementPresets != null) settings.RefinementPresets = RefinementPresets.ToList();
             settings.SelectedRefinementPresetId = SelectedRefinementPreset?.Id;
             
             settings.IsPostProcessingEnabled = IsPostProcessingEnabled;
             settings.IsAlwaysOnTop = IsAlwaysOnTop;
             settings.HotkeyCodes = _currentHotkeyCodes;
             settings.CloseToTray = CloseToTray;
             settings.ActivationMode = ActivationMode;
             settings.PlaySoundOnRecord = PlaySoundOnRecord;
             settings.PauseMediaOnRecording = PauseMediaOnRecording;
             settings.TextInjectionMode = TextInjectionMode;
             settings.TextInjectionMode = TextInjectionMode;
             settings.SelectedMicrophone = SelectedAudioDevice;
             settings.IsAutoInsertEnabled = IsAutoInsertEnabled;
             
             await _settingsService.SaveSettingsAsync(settings);

             // Notify others, but they should also be careful not to re-trigger us
             WeakReferenceMessenger.Default.Send(new SettingsChangedMessage());
        }

        public void HandleHotkeyInput(Key key)
        {
            if (!IsRecordingHotkey) return;
            
            // ... Logic same as before ...
            // Wait, does MainViewModel DO hotkey recording? 
            // The logic was in SettingsWindow previously, but bound to MainViewModel.
            // Now SettingsWindow binds to SettingsViewModel.
            // Main Window usually doesn't record hotkeys?
            // If the user wants to set hotkey, they go to Settings.
            // So MainViewModel probably doesn't need HandleHotkeyInput anymore!
            
            // BUT: StartNewHotkeyCapture command exists.
            // If there's no UI for it in Main Window, we can remove it.
            // Usually hotkey setting is in Settings.
        }
        
        // Removing HandleHotkeyInput and StartNewHotkeyCapture as they belong to SettingsViewModel now.
        // Unless Main Window has a Quick Hotkey Set button? unlikely.

        private bool IsModifier(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LWin || key == Key.RWin;
        }

        private void UpdateHotkeyDisplay()
        {
            if (_currentHotkeyCodes.Count == 0) _currentHotkeyCodes.Add(0x14);

            var names = _currentHotkeyCodes.Select(vk => ((Key)KeyInterop.KeyFromVirtualKey(vk)).ToString());
            HotkeyDisplay = string.Join(" + ", names).Replace("Capital", "CapsLock");
            UpdateInstructionText();
        }

        partial void OnIsAlwaysOnTopChanged(bool value) => _ = SaveSettingsAsync();
        // Other operational settings saving...
        partial void OnSelectedTranscriptionProfileChanged(ProviderProfile? value) => _ = SaveSettingsAsync();
        partial void OnSelectedRefinementPresetChanged(RefinementPreset? value) => _ = SaveSettingsAsync();
        // ... if Main UI allows changing these.

        private void RefreshFilteredCollections()
        {
            OnPropertyChanged(nameof(AvailableTranscriptionProfiles));
            OnPropertyChanged(nameof(AvailableRefinementProfiles));
        }

        // ... (Remaining Recording/Processing methods identical to before) ...
        
        private async void OnHotkeyDown()
        {
             // ... Same as before ...
             if (ActivationMode == 0) // Tap to Talk
             {
                 if (_audioRecorder.IsRecording)
                 {
                     await StopAndProcessInternal();
                 }
                 else
                 {
                     if (IsProcessing) return;
                     await StartRecordingInternal();
                 }
             }
             else // Push to Talk
             {
                 if (_audioRecorder.IsRecording || IsProcessing) return;
                 await StartRecordingInternal();
             }
        }

        private async Task StartRecordingInternal()
        {
            if (SelectedTranscriptionProfile == null)
            {
                Status = "Error: No Provider";
                return;
            }

            if (PauseMediaOnRecording)
            {
                _mediaWasPausedByUs = await _systemControl.PauseMediaIfPlayingAsync();
            }

            await PlaySoundAsync("start.wav");

            _logger.LogInfo("Recording started.");
            Status = "Recording...";
            UiState = "Listening";
            LastTranscription = "";
            _recordingStartTime = DateTime.Now;
            _recordingTimer?.Start();
            
            _targetWindowHandle = _systemControl.GetForegroundWindowHandle();
            _logger.LogInfo($"Captured target window handle: {_targetWindowHandle}");

            IsProcessing = true;

            try
            {
                if (_audioRecorder is FluentDraft.Services.Implementations.NAudioRecorder recorder)
                {
                    recorder.DeviceNumber = SelectedAudioDevice;
                }
                _audioRecorder.StartRecording();
            }
            catch (Exception ex)
            {
                _logger.LogError("Start Record failed", ex);
                Status = "Error: Start Record failed";
                IsProcessing = false;
            }
        }

        private async void OnHotkeyUp()
        {
            if (ActivationMode == 0) return;
            await StopAndProcessInternal();
        }

        private async Task StopAndProcessInternal(bool wasAutoStopped = false)
        {
            if (!_audioRecorder.IsRecording) return;

            _logger.LogInfo("Recording stopped, processing...");
            await _audioRecorder.StopRecordingAsync();
            
            await PlaySoundAsync("stop.wav");

            if (PauseMediaOnRecording && _mediaWasPausedByUs)
            {
                await _systemControl.ResumeMediaAsync();
                _mediaWasPausedByUs = false;
            }
            
            _recordingTimer?.Stop();
            
            if (wasAutoStopped) Status = $"Limit ({MaxRecordingSeconds}s)";
            
            _processingStartTime = DateTime.Now;
            UiState = "Transcribing";
            Status = "Transcribing...";

            _processingCts = new CancellationTokenSource();
            
            try
            {
                var filePath = _audioRecorder.GetRecordedFilePath();
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Status = "Error: No recording";
                    IsProcessing = false;
                    return;
                }

                if (new FileInfo(filePath).Length < 8192)
                {
                    Status = "Too short";
                    IsProcessing = false;
                    _ = Task.Delay(1000).ContinueWith(_ => Status = "Ready");
                    return;
                }

                var profile = SelectedTranscriptionProfile;
                if (profile == null) throw new Exception("No transcription provider selected.");
                
                string endpoint = profile.BaseUrl;
                string key = profile.ApiKey;
                string model = profile.TranscriptionModel;

                var text = await _transcriptionService.TranscribeAsync(filePath, key, endpoint, model);
                
                if (_processingCts.Token.IsCancellationRequested) return;

                if (IsPostProcessingEnabled && SelectedRefinementPreset != null)
                {
                    Status = "Refining...";
                    
                    var refProfile = Providers.FirstOrDefault(p => p.Id == SelectedRefinementPreset.ProfileId);
                    if (refProfile != null)
                    {
                        string rEndpoint = refProfile.BaseUrl.TrimEnd('/');
                        if (!rEndpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) 
                        {
                            rEndpoint += "/chat/completions";
                        }
                       
                        var refModel = !string.IsNullOrEmpty(SelectedRefinementPreset.Model) 
                            ? SelectedRefinementPreset.Model 
                            : refProfile.RefinementModel;
                        var prompt = SelectedRefinementPreset.SystemPrompt;
                       
                        text = await _textProcessor.ProcessTextAsync(text, prompt, refProfile.ApiKey, rEndpoint, refModel, _chatSessionId);
                    }
                }

                LastTranscription = text;
                await AddToHistory(text, filePath);
                Status = "Done";
                UiState = "Done";
                var processingElapsed = DateTime.Now - _processingStartTime;
                ProcessingTimeDisplay = $"{processingElapsed.TotalSeconds:F1}s".Replace(".", ",");

                if (IsAutoInsertEnabled)
                {
                    await _inputInjector.TypeTextAsync(text, TextInjectionMode == 1);
                    _logger.LogInfo("Text injected automatically.");
                }
                else
                {
                    _logger.LogInfo("Auto-insert disabled. Waiting for user action.");
                }

                _ = Task.Delay(3000).ContinueWith(_ => 
                {
                    if (UiState == "Done")
                    {
                        UiState = "None";
                        Status = "Ready";
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Flow failed", ex);
                Status = $"Error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                _processingCts?.Dispose();
                _processingCts = null;
            }
        }

        private async Task AddToHistory(string text, string audioPath)
        {
            var item = new TranscriptionItem
            {
                Text = text,
                AudioFilePath = audioPath,
                Timestamp = DateTime.Now
            };
            await _historyService.AddAsync(item);
            System.Windows.Application.Current.Dispatcher.Invoke(() => HistoryItems.Insert(0, item));
        }

         private async Task DeleteHistoryItem(TranscriptionItem item)
        {
             await _historyService.DeleteAsync(item);
            HistoryItems.Remove(item);
             HasSelectedItems = HistoryItems.Any(i => i.IsSelected);
        }
        private async Task ClearHistory()
        {
            await _historyService.ClearAsync();
            HistoryItems.Clear();
             HasSelectedItems = false;
        }

        private async void ExecuteManualInsert()
        {
            if (string.IsNullOrEmpty(LastTranscription)) return;

            if (_targetWindowHandle != IntPtr.Zero)
            {
                _systemControl.SetForegroundWindow(_targetWindowHandle);
                await Task.Delay(100); // Wait for focus switch
                await _inputInjector.TypeTextAsync(LastTranscription, TextInjectionMode == 1);
                _logger.LogInfo("Text injected manually.");
            }
            else
            {
                 // Fallback if handle invalid? Maybe just copy?
                 // Or try to inject anyway (might go to self if active)
                 // But wait, if user clicked, self IS active.
                 // So we must have a handle.
                 
                System.Windows.MessageBox.Show("Target window lost. Copied to clipboard instead.");
                CopyLastTranscription();
            }
        }
        
        partial void OnIsAutoInsertEnabledChanged(bool value) => _ = SaveSettingsAsync();

        private void CopyHistoryItem(TranscriptionItem item)
        {
            if(!string.IsNullOrEmpty(item.Text)) System.Windows.Clipboard.SetText(item.Text);
        }
        private void PlayHistoryItem(TranscriptionItem item)
        {
             if(File.Exists(item.AudioFilePath))
            {
                Task.Run(() => new System.Media.SoundPlayer(item.AudioFilePath).Play());
            }
        }
        
        private void SelectAllHistory()
        {
            foreach(var item in HistoryItems) item.IsSelected = true;
            OnPropertyChanged(nameof(HasSelectedItems));
        }
        private void DeselectAllHistory()
        {
            foreach(var item in HistoryItems) item.IsSelected = false;
             OnPropertyChanged(nameof(HasSelectedItems));
        }
        private void CopySelectedHistory()
        {
            var selected = HistoryItems.Where(i => i.IsSelected).Select(i => i.Text);
            if(selected.Any())
            {
                 System.Windows.Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, selected));
            }
        }

        private async void InitializeHistory()
        {
            var items = await _historyService.GetHistoryAsync();
             HistoryItems = new ObservableCollection<TranscriptionItem>(items);
             foreach(var item in HistoryItems)
             {
                 item.PropertyChanged += (s, e) => 
                 {
                     if(e.PropertyName == nameof(TranscriptionItem.IsSelected))
                        OnPropertyChanged(nameof(HasSelectedItems));
                 };
             }
        }

        private async Task PlaySoundAsync(string fileName)
        {
            if (!PlaySoundOnRecord) return;
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", fileName);
                if (File.Exists(path))
                {
                    await Task.Run(() => new System.Media.SoundPlayer(path).PlaySync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to play sound {fileName}", ex);
            }
        }

        private void OnVolumeChanged(double e)
        {
            VolumeLevel = e;
            if (_audioRecorder.IsRecording)
            {
                 System.Windows.Application.Current.Dispatcher.Invoke(() =>
                 {
                     var rand = new Random();
                     foreach (var bar in AudioWaves)
                     {
                         var targetHeight = (VolumeLevel * 300) * (rand.NextDouble() * 0.5 + 0.5); 
                         targetHeight = Math.Max(3, Math.Min(40, targetHeight));
                         bar.Height = targetHeight;
                     }
                 });
            }
            else
            {
                 System.Windows.Application.Current.Dispatcher.Invoke(() =>
                 {
                     foreach (var bar in AudioWaves) bar.Height = 3;
                 });
            }
        }

        private void LoadAudioDevices()
        {
             var devices = _audioDeviceService.GetRecordingDevices();
             AudioDevices.Clear();
             foreach(var (n, name) in devices)
             {
                 AudioDevices.Add(new AudioDeviceModel { DeviceNumber = n, ProductName = name });
             }
        }

        private void OpenSettingsWindow()
        {
             IsSettingsVisible = true;
        }

        private void CloseSettingsWindow()
        {
            // Just close, settings are saved by SettingsViewModel or explicit actions
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is FluentDraft.Views.SettingsWindow)
                {
                    window.Close();
                    return;
                }
            }
        }
    }
}
