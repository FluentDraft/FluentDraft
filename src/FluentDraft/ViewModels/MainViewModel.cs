using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FluentDraft.Models;
using FluentDraft.Services;
using FluentDraft.Services.Interfaces;
using FluentDraft.Utils;

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

    public partial class MainViewModel : ObservableObject
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
        private readonly IUpdateService _updateService; // Added
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _status = "Ready";

        // Providers
        [ObservableProperty]
        private ObservableCollection<ProviderProfile> _providers = new();

        [ObservableProperty]
        private ProviderProfile? _selectedTranscriptionProfile;

        [ObservableProperty]
        private ProviderProfile? _selectedRefinementProfile;

        [ObservableProperty]
        private ProviderProfile? _selectedEditingProvider;

        // Refinement Presets
        [ObservableProperty]
        private ObservableCollection<RefinementPreset> _refinementPresets = new();

        [ObservableProperty]
        private RefinementPreset? _selectedRefinementPreset;

        [ObservableProperty]
        private RefinementPreset? _selectedEditingPreset;

        public ObservableCollection<ProviderProfile> AvailableTranscriptionProfiles => 
            new ObservableCollection<ProviderProfile>(Providers.Where(p => p.IsTranscriptionEnabled));

        public ObservableCollection<ProviderProfile> AvailableRefinementProfiles => 
            new ObservableCollection<ProviderProfile>(Providers.Where(p => p.IsRefinementEnabled));
        
        // Commands for Profile Management
        public RelayCommand AddProviderCommand { get; }
        public RelayCommand<ProviderProfile> RemoveProviderCommand { get; }
        public RelayCommand<ProviderProfile> TestProviderCommand { get; }
        public RelayCommand<ProviderProfile> FetchModelsCommand { get; }

        // Commands for Preset Management
        public RelayCommand AddPresetCommand { get; }
        public RelayCommand<RefinementPreset> RemovePresetCommand { get; }

        [ObservableProperty]
        private string _logs = "";

        [ObservableProperty]
        private bool _isAlwaysOnTop = false;

        [ObservableProperty]
        private bool _isPostProcessingEnabled = true;

        // PostProcessingPrompt is now legacy, use SelectedRefinementPreset.SystemPrompt instead
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


        private System.Windows.Threading.DispatcherTimer? _recordingTimer;
        private DateTime _recordingStartTime;
        private DateTime _processingStartTime;
        private bool _mediaWasPausedByUs = false;

        public ObservableCollection<string> AvailableProviderTypes { get; } = new() { "Groq", "OpenAI", "Custom" };

        // About properties
        public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        public string DotNetVersion => Environment.Version.ToString();
        public string OsVersion => $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";

        // Update Properties
        [ObservableProperty]
        private string _updateStatus = "Check for updates";

        [ObservableProperty]
        private bool _isCheckingForUpdates = false;

        private List<int> _currentHotkeyCodes = new List<int> { 0x14 };
        private CancellationTokenSource? _processingCts;

        public RelayCommand StartRecordingHotkeyCommand { get; }
        public RelayCommand ToggleSettingsCommand { get; }
        public RelayCommand CloseSettingsCommand { get; }
        public RelayCommand CheckForUpdatesCommand { get; } // Added
        
        private void RequestCloseSettings()
        {
            if (!ValidateSettings()) return;
            CloseSettingsWindow();
        }

        private bool ValidateSettings()
        {
            // Validate Transcription
            if (SelectedTranscriptionProfile != null && SelectedTranscriptionProfile.IsTranscriptionEnabled)
            {
                if (string.IsNullOrWhiteSpace(SelectedTranscriptionProfile.TranscriptionModel))
                {
                    System.Windows.MessageBox.Show("Please select a valid 'Transcription Model' in the Transcription tab.", "Settings Invalid", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return false;
                }
            }

            // Validate Refinement
            if (IsPostProcessingEnabled && SelectedRefinementProfile != null)
            {
                if (string.IsNullOrWhiteSpace(SelectedRefinementProfile.RefinementModel))
                {
                    System.Windows.MessageBox.Show("Refinement is enabled but no 'Refinement Model' is selected.\nPlease select a model in the Refinement tab.", "Settings Invalid", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

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
            _updateService = updateService; // Added
            _serviceProvider = serviceProvider;

            LoadAudioDevices();

            // Initialize 60 bars for the wave animation (High fidelity)
            for (int i = 0; i < 60; i++)
            {
                AudioWaves.Add(new WaveBar { Height = 3 }); // Start as dots
            }

            StartRecordingHotkeyCommand = new RelayCommand(StartNewHotkeyCapture);
            ToggleSettingsCommand = new RelayCommand(OpenSettingsWindow);
            CloseSettingsCommand = new RelayCommand(RequestCloseSettings);
            CopyLastTranscriptionCommand = new RelayCommand(CopyLastTranscription);
            CancelCommand = new RelayCommand(CancelProcessing);
            StartManualRecordingCommand = new RelayCommand(ToggleRecordingManual);
            SetTapToTalkCommand = new RelayCommand(() => ActivationMode = 0);
            SetPushToTalkCommand = new RelayCommand(() => ActivationMode = 1);
            
            AddProviderCommand = new RelayCommand(AddProvider);
            RemoveProviderCommand = new RelayCommand<ProviderProfile>(RemoveProvider);
            TestProviderCommand = new RelayCommand<ProviderProfile>(async (p) => await TestProvider(p));
            FetchModelsCommand = new RelayCommand<ProviderProfile>(async (p) => await FetchModels(p));
            CheckForUpdatesCommand = new RelayCommand(async () => await CheckForUpdates()); // Added

            AddPresetCommand = new RelayCommand(AddPreset);
            RemovePresetCommand = new RelayCommand<RefinementPreset>(RemovePreset);

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

            LoadSettings();
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
                
                // Auto-stop when limit reached
                if (elapsed.TotalSeconds >= MaxRecordingSeconds)
                {
                    _logger.LogInfo($"Recording limit reached ({MaxRecordingSeconds}s). Auto-stopping.");
                    _ = StopAndProcessInternal(wasAutoStopped: true);
                }
            };

            _logger.LogInfo("MainViewModel initialized with Provider Profiles.");
        }

        private void AddProvider()
        {
            var newProfile = new ProviderProfile { Name = "New Provider", Type = "Groq", IsTranscriptionEnabled=true, IsRefinementEnabled=true };
            Providers.Add(newProfile);
            SelectedEditingProvider = newProfile; // Auto-select for editing
            SaveSettings();
            RefreshFilteredCollections();
        }

        private void RemoveProvider(ProviderProfile? profile)
        {
            if (profile == null) return;
            Providers.Remove(profile);
            if (SelectedEditingProvider == profile) SelectedEditingProvider = null;
            if (SelectedTranscriptionProfile == profile) SelectedTranscriptionProfile = Providers.FirstOrDefault(p => p.IsTranscriptionEnabled);
            if (SelectedRefinementProfile == profile) SelectedRefinementProfile = Providers.FirstOrDefault(p => p.IsRefinementEnabled);
            
            // Update any presets that reference this profile
            foreach (var preset in RefinementPresets.Where(p => p.ProfileId == profile.Id))
            {
                preset.ProfileId = null;
            }
            
            SaveSettings();
            RefreshFilteredCollections();
        }

        private void AddPreset()
        {
            var defaultProfile = Providers.FirstOrDefault(p => p.IsRefinementEnabled);
            var newPreset = new RefinementPreset 
            { 
                Name = "New Preset",
                ProfileId = defaultProfile?.Id,
                Model = defaultProfile?.RefinementModel ?? "",
                SystemPrompt = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments."
            };
            RefinementPresets.Add(newPreset);
            SelectedEditingPreset = newPreset;
            SaveSettings();
        }

        private void RemovePreset(RefinementPreset? preset)
        {
            if (preset == null || RefinementPresets.Count <= 1) return; // Keep at least one preset
            RefinementPresets.Remove(preset);
            if (SelectedEditingPreset == preset) SelectedEditingPreset = null;
            if (SelectedRefinementPreset == preset) SelectedRefinementPreset = RefinementPresets.FirstOrDefault();
            SaveSettings();
        }

        private async Task FetchModels(ProviderProfile? profile)
        {
            if (profile == null) return;
            
            _logger.LogInfo($"Fetching models for {profile.Name}...");
            try 
            {
                var models = await _transcriptionService.GetAvailableModelsAsync(profile.ApiKey, profile.BaseUrl);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    UpdateProfileModels(profile, models);
                });
                
                _logger.LogInfo($"Fetched {models.Count} models for {profile.Name}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to fetch models for {profile.Name}", ex);
                System.Windows.MessageBox.Show($"Failed to fetch models:\n{ex.Message}", "Fetch Models", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task TestProvider(ProviderProfile? profile)
        {
            if (profile == null) return;
            
            _logger.LogInfo($"Testing connection for {profile.Name} ({profile.BaseUrl})...");
            try 
            {
                var models = await _transcriptionService.GetAvailableModelsAsync(profile.ApiKey, profile.BaseUrl);
                _logger.LogInfo($"Connection test successful. Found {models.Count} models.");
                
                profile.IsValidated = true; // Mark as validated
                OnPropertyChanged(nameof(Providers)); 

                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    UpdateProfileModels(profile, models);
                });

                string msg = $"Connection successful!\nFound {models.Count} compatible models.";
                if(models.Count > 0) msg += $"\nFirst: {models[0]}";
                
                System.Windows.MessageBox.Show(msg, "Test Connection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                profile.IsValidated = false;
                _logger.LogError($"Connection test failed for {profile.Name}", ex);
                System.Windows.MessageBox.Show($"Connection failed:\n{ex.Message}", "Test Connection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            // Explicit save to persist validation status
            SaveSettings();
        }

        private void UpdateProfileModels(ProviderProfile profile, List<string> models)
        {
            var currentTrans = profile.TranscriptionModel;
            var currentRef = profile.RefinementModel;

            profile.TranscriptionModels.Clear();
            profile.RefinementModels.Clear();

            foreach (var m in models)
            {
                if (m.IndexOf("whisper", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    profile.TranscriptionModels.Add(m);
                }
                else
                {
                    profile.RefinementModels.Add(m);
                }
            }
            
            // Preserve selection if it exists in the new list, otherwise might default or leave empty
            if (!string.IsNullOrEmpty(currentTrans) && profile.TranscriptionModels.Contains(currentTrans))
            {
                profile.TranscriptionModel = currentTrans;
            }
            if (!string.IsNullOrEmpty(currentRef) && profile.RefinementModels.Contains(currentRef))
            {
                profile.RefinementModel = currentRef;
            }
        }

        private void RefreshFilteredCollections()
        {
            OnPropertyChanged(nameof(AvailableTranscriptionProfiles));
            OnPropertyChanged(nameof(AvailableRefinementProfiles));
        }

        private async void ToggleRecordingManual()
        {
            if (_audioRecorder.IsRecording) await StopAndProcessInternal();
            else OnHotkeyDown();
        }

        [ObservableProperty]
        private bool _isCopiedNotificationVisible;

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
            if (_audioRecorder.IsRecording)
            {
                _audioRecorder.StopRecordingAsync();
                _logger.LogInfo("Recording cancelled by user.");
            }
            
            _processingCts?.Cancel();
            IsProcessing = false;
            Status = "Cancelled";
            _ = Task.Delay(2000).ContinueWith(_ => Status = "Ready");
        }

        private void LoadSettings()
        {
            var settings = _settingsService.LoadSettings();
            
            // Migration Logic for Providers
            if (settings.Providers == null || !settings.Providers.Any())
            {
                var defaultProfile = new ProviderProfile
                {
                    Name = "Default Groq",
                    Type = "Groq",
                    BaseUrl = "https://api.groq.com/openai/v1",
                    TranscriptionModel = "whisper-large-v3",
                    RefinementModel = "llama3-70b-8192",
                    IsTranscriptionEnabled = true,
                    IsRefinementEnabled = true
                };
                settings.Providers = new List<ProviderProfile> { defaultProfile };
                settings.SelectedTranscriptionProfileId = defaultProfile.Id;
                settings.SelectedRefinementProfileId = defaultProfile.Id;
            }

            Providers = new ObservableCollection<ProviderProfile>(settings.Providers);
            
            SelectedTranscriptionProfile = Providers.FirstOrDefault(p => p.Id == settings.SelectedTranscriptionProfileId) ?? Providers.FirstOrDefault(p => p.IsTranscriptionEnabled);
            SelectedRefinementProfile = Providers.FirstOrDefault(p => p.Id == settings.SelectedRefinementProfileId) ?? Providers.FirstOrDefault(p => p.IsRefinementEnabled);

            // Migration Logic for Presets
            if (settings.RefinementPresets == null || !settings.RefinementPresets.Any())
            {
                var defaultProfile = Providers.FirstOrDefault(p => p.IsRefinementEnabled);
                var defaultPreset = new RefinementPreset
                {
                    Name = "Default",
                    ProfileId = defaultProfile?.Id,
                    Model = defaultProfile?.RefinementModel ?? "llama3-70b-8192",
                    SystemPrompt = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments."
                };
                
                var chatPreset = new RefinementPreset
                {
                    Name = "Chat Mode",
                    ProfileId = defaultProfile?.Id,
                    Model = defaultProfile?.RefinementModel ?? "llama3-70b-8192",
                    SystemPrompt = "You are a text cleaner for chat messages. Your task is to ONLY format the input text to look natural and casual. Remove filler words (like 'um', 'uh') and fix typos. Use lowercase where appropriate for a casual vibe. minimal punctuation. Do NOT add a period at the end. Do NOT answer the user. Do NOT add any new information or expand the thought. OUTPUT ONLY THE REFINED TEXT."
                };

                settings.RefinementPresets = new List<RefinementPreset> { defaultPreset, chatPreset };
                settings.SelectedRefinementPresetId = defaultPreset.Id;
            }
            else
            {
                // Ensure Chat Mode exists if it's missing (for existing users)
                var chatParams = settings.RefinementPresets.FirstOrDefault(p => p.Name == "Chat Mode");
                if (chatParams == null)
                {
                    var defaultProfile = Providers.FirstOrDefault(p => p.IsRefinementEnabled);
                    settings.RefinementPresets.Add(new RefinementPreset
                    {
                        Name = "Chat Mode",
                        ProfileId = defaultProfile?.Id,
                        Model = defaultProfile?.RefinementModel ?? "llama3-70b-8192",
                        SystemPrompt = "You are a text cleaner for chat messages. Your task is to ONLY format the input text to look natural and casual. Remove filler words (like 'um', 'uh') and fix typos. Use lowercase where appropriate for a casual vibe. minimal punctuation. Do NOT add a period at the end. Do NOT answer the user. Do NOT add any new information or expand the thought. OUTPUT ONLY THE REFINED TEXT."
                    });
                }
                else
                {
                    // Update existing Chat Mode prompt to the fixed version
                    chatParams.SystemPrompt = "You are a text cleaner for chat messages. Your task is to ONLY format the input text to look natural and casual. Remove filler words (like 'um', 'uh') and fix typos. Use lowercase where appropriate for a casual vibe. minimal punctuation. Do NOT add a period at the end. Do NOT answer the user. Do NOT add any new information or expand the thought. OUTPUT ONLY THE REFINED TEXT.";
                }

                // Update Default prompt if it's the old legacy one or matches the user request to fix it
                var defaultPreset = settings.RefinementPresets.FirstOrDefault(p => p.Name == "Default");
                if (defaultPreset != null)
                {
                     // Update to the new prompt without [INPUT_TEXT] tag reference
                     defaultPreset.SystemPrompt = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments.";
                }
            }

            RefinementPresets = new ObservableCollection<RefinementPreset>(settings.RefinementPresets);
            SelectedRefinementPreset = RefinementPresets.FirstOrDefault(p => p.Id == settings.SelectedRefinementPresetId) ?? RefinementPresets.FirstOrDefault();

            IsAlwaysOnTop = settings.IsAlwaysOnTop;
            CloseToTray = settings.CloseToTray;
            IsPostProcessingEnabled = settings.IsPostProcessingEnabled;
            PostProcessingPrompt = settings.PostProcessingPrompt; // Keep for legacy
            PlaySoundOnRecord = settings.PlaySoundOnRecord;
            PauseMediaOnRecording = settings.PauseMediaOnRecording;
            ActivationMode = settings.ActivationMode;
            TextInjectionMode = settings.TextInjectionMode;
            SelectedAudioDevice = settings.SelectedMicrophone;
            MaxRecordingSeconds = settings.MaxRecordingSeconds;

            _currentHotkeyCodes = settings.HotkeyCodes ?? new List<int> { 0x14 };
            UpdateHotkeyDisplay();
            // UpdateInstructionText is called inside UpdateHotkeyDisplay, but ActivationMode might be set after?
            // Safe to call again or rely on property setters?
            // ActivationMode is set in LoadSettings before this line. 
            // But UpdateInstructionText depends on HotkeyDisplay which is set in UpdateHotkeyDisplay.
            // So calling it inside UpdateHotkeyDisplay is correct.
            _hotkeyManager.SetMonitoredKeys(_currentHotkeyCodes);
            
            RefreshFilteredCollections();
            
            _settingsService.SaveSettings(settings); // Save any migration
        }

        public void SaveSettings()
        {
            var settings = new AppSettings
            {
                Providers = Providers.ToList(),
                SelectedTranscriptionProfileId = SelectedTranscriptionProfile?.Id,
                SelectedRefinementProfileId = SelectedRefinementProfile?.Id,
                
                RefinementPresets = RefinementPresets.ToList(),
                SelectedRefinementPresetId = SelectedRefinementPreset?.Id,
                
                IsPostProcessingEnabled = IsPostProcessingEnabled,
                PostProcessingPrompt = SelectedRefinementPreset?.SystemPrompt ?? PostProcessingPrompt, // Keep in sync
                
                IsAlwaysOnTop = IsAlwaysOnTop,
                HotkeyCodes = _currentHotkeyCodes,
                CloseToTray = CloseToTray,
                ActivationMode = ActivationMode,
                PlaySoundOnRecord = PlaySoundOnRecord,
                PauseMediaOnRecording = PauseMediaOnRecording,
                TextInjectionMode = TextInjectionMode,
                SelectedMicrophone = SelectedAudioDevice,
                MaxRecordingSeconds = MaxRecordingSeconds
            };
            _settingsService.SaveSettings(settings);
            _hotkeyManager.SetMonitoredKeys(_currentHotkeyCodes);
            
            RefreshFilteredCollections();
        }

        public void HandleHotkeyInput(Key key)
        {
            if (!IsRecordingHotkey) return;

            int vk = KeyInterop.VirtualKeyFromKey(key);
            if (vk == 0) return;

            if (_currentHotkeyCodes.Count > 0 && !IsModifier(key))
            {
                if (!_currentHotkeyCodes.Contains(vk)) _currentHotkeyCodes.Add(vk);
                IsRecordingHotkey = false;
                UpdateHotkeyDisplay();
                _hotkeyManager.SetMonitoredKeys(_currentHotkeyCodes);
                SaveSettings();
            }
            else
            {
                if (!IsModifier(key)) 
                {
                    _currentHotkeyCodes = new List<int> { vk };
                    IsRecordingHotkey = false;
                    UpdateHotkeyDisplay();
                    SaveSettings();
                }
                else
                {
                    if (_currentHotkeyCodes.Contains(vk)) return;
                    _currentHotkeyCodes.Add(vk);
                    UpdateHotkeyDisplay();
                }
            }
        }

        public void StartNewHotkeyCapture()
        {
            _currentHotkeyCodes.Clear();
            HotkeyDisplay = "Press keys...";
            IsRecordingHotkey = true;
        }

        private bool IsModifier(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LWin || key == Key.RWin;
        }

        private void UpdateHotkeyDisplay()
        {
            if (_currentHotkeyCodes.Count == 0)
            {
                _currentHotkeyCodes.Add(0x14); // Fallback to CapsLock matches GlobalHotkeyManager
            }

            var names = _currentHotkeyCodes.Select(vk => ((Key)KeyInterop.KeyFromVirtualKey(vk)).ToString());
            HotkeyDisplay = string.Join(" + ", names).Replace("Capital", "CapsLock");
            UpdateInstructionText();
        }

        partial void OnIsAlwaysOnTopChanged(bool value) => SaveSettings();
        partial void OnIsPostProcessingEnabledChanged(bool value) => SaveSettings();
        partial void OnPostProcessingPromptChanged(string value) => SaveSettings();
        partial void OnActivationModeChanged(int value) 
        {
            UpdateInstructionText();
            SaveSettings();
        }
        partial void OnTextInjectionModeChanged(int value) => SaveSettings();
        partial void OnPlaySoundOnRecordChanged(bool value) => SaveSettings();
        partial void OnCloseToTrayChanged(bool value) => SaveSettings();
        partial void OnPauseMediaOnRecordingChanged(bool value) => SaveSettings();
        partial void OnSelectedAudioDeviceChanged(int value) => SaveSettings();
        
        partial void OnSelectedTranscriptionProfileChanged(ProviderProfile? value) => SaveSettings();
        partial void OnSelectedRefinementProfileChanged(ProviderProfile? value) => SaveSettings();
        partial void OnSelectedRefinementPresetChanged(RefinementPreset? value) => SaveSettings();
        
        partial void OnSelectedEditingPresetChanged(RefinementPreset? value)
        {
            if (value != null)
            {
                _ = FetchPresetModelsAsync(value);
            }
        }
        
        private async Task FetchPresetModelsAsync(RefinementPreset preset)
        {
            var profile = Providers.FirstOrDefault(p => p.Id == preset.ProfileId);
            if (profile == null) return;
            
            // Save current model before fetching
            var currentModel = preset.Model;
            
            try
            {
                _logger.LogInfo($"Fetching text models for preset '{preset.Name}'...");
                var allModels = await _transcriptionService.GetAvailableModelsAsync(profile.ApiKey, profile.BaseUrl);
                
                // Filter to only text-processing models (exclude whisper, audio, video, embedding models)
                var textModels = allModels.Where(m => 
                    !m.Contains("whisper", StringComparison.OrdinalIgnoreCase) &&
                    !m.Contains("audio", StringComparison.OrdinalIgnoreCase) &&
                    !m.Contains("video", StringComparison.OrdinalIgnoreCase) &&
                    !m.Contains("vision", StringComparison.OrdinalIgnoreCase) &&
                    !m.Contains("embed", StringComparison.OrdinalIgnoreCase) &&
                    !m.Contains("tts", StringComparison.OrdinalIgnoreCase) &&
                    !m.Contains("playai", StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Build new list without clearing first to avoid UI reset
                    var newModels = new List<string>(textModels);
                    
                    // If current model is not in list, add it at the beginning
                    if (!string.IsNullOrEmpty(currentModel) && !newModels.Contains(currentModel))
                    {
                        newModels.Insert(0, currentModel);
                    }
                    
                    // Now update the collection
                    preset.AvailableModels.Clear();
                    foreach (var model in newModels)
                    {
                        preset.AvailableModels.Add(model);
                    }
                    
                    // Restore the model selection after updating the list
                    if (!string.IsNullOrEmpty(currentModel))
                    {
                        preset.Model = currentModel;
                    }
                    
                    _logger.LogInfo($"Found {textModels.Count} text models for preset.");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to fetch models for preset: {ex.Message}");
            }
        }

        private async Task CheckForUpdates()
        {
            if (IsCheckingForUpdates) return;

            try
            {
                IsCheckingForUpdates = true;
                UpdateStatus = "Checking for updates...";
                
                // Only check, don't auto apply yet
                var updateInfo = await _updateService.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    UpdateStatus = "No updates available.";
                    await Task.Delay(3000);
                    UpdateStatus = "Check for updates";
                }
                else
                {
                     UpdateStatus = $"Found v{updateInfo.TargetFullRelease.Version}! Downloading...";
                     
                     // Download and Restart
                     await _updateService.DownloadUpdateAsync(updateInfo);
                     UpdateStatus = "Installing...";
                     _updateService.ApplyUpdateAndRestart(updateInfo);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus = "Update failed.";
                _logger.LogError("Manual update check failed", ex);
                System.Windows.MessageBox.Show($"Update check failed: {ex.Message}", "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        // Listen for changes INSIDE the profiles? 
        // ObservableCollection doesn't track property changes of items.
        // We might need to manually trigger Save if a user edits a profile field.
        // For now, assume 'Save' button in UI (or closing settings) handles it, or explicit Save.
        // Actually, we should probably add a "Save Providers" or just rely on Close to save everything.
        // The SaveSettings method does dump the entire list.
        
        private async void OnHotkeyDown()
        {
            if (IsRecordingHotkey) return;

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
            
            // Notify user if recording was auto-stopped due to limit
            if (wasAutoStopped)
            {
                Status = $"Limit ({MaxRecordingSeconds}s)";
            }
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

                _logger.LogInfo($"Transcribing with {profile.Name} ({profile.Type})...");
                
                // We need to resolve the endpoint based on Type if BaseUrl is empty? 
                // Or assume user filled it.
                // NOTE: The user wants "Standard" types like OpenAI/Groq to just work. 
                // We should probably pre-fill BaseUrl when Type changes in UI, or resolve it here.
                // Let's rely on the BaseUrl being stored in the Profile.
                
                string endpoint = profile.BaseUrl; // e.g., https://api.groq.com/openai/v1
                string key = profile.ApiKey;
                string model = profile.TranscriptionModel; // e.g., whisper-large-v3

                // NOTE: The current TranscriptionService (OpenAiCompatible) expects a BaseUrl that includes /v1 if it's generic, 
                // but checking the previous implementation, it passed "https://api.groq.com/openai/v1"
                
                // Adjust for OpenAITranscriptionService logic if needed. 
                // The current ITranscriptionService.TranscribeAsync(file, key) doesn't take URL/Model?
                // Wait, I need to check how ITranscriptionService is injected. 
                // If it's the generic one, we might need to configure it per request or passed in.
                // Inspecting the previous code: `_transcriptionService.TranscribeAsync(filePath, ApiKey)`
                // The previous code SET `TranscriptionBaseUrl` and `CurrentModel` properties on the ViewModel, 
                // but DID NOT pass them to `TranscribeAsync`.
                // This implies the `ITranscriptionService` was either stateful (bad) or those properties were unused?
                // OR `ITranscriptionService` is actually `OpenAiCompatibleTranscriptionService` and it reads from settings?
                // Let's assume for now we need to Refactor ITranscriptionService to accept Config, 
                // OR we update the Service to read the *current* settings.
                
                // Checking previous: `_transcriptionService` was just used as `TranscribeAsync(path, key)`.
                // If the service is generic, it MUST know the URL/Model.
                // If the service pulls from AppSettings singleton, we are good because we updated AppSettings.
                // BUT, we changed AppSettings structure. 
                // StartTranscriptionInternal needs to ensure the Service uses the parameters from `SelectedTranscriptionProfile`.
                
                // HACK: Start 'TranscribeAsync' assumes the service knows what to do. 
                // If the service injects ISettingsService, it might break because we changed AppSettings.
                // I will need to fix the Service implementation too if it relies on IsItemsService.
                
                // Assuming ITranscriptionService is now Generic and requires configuration passed in, 
                // or we need to update the service to look at `settings.SelectedTranscriptionProfile`.
                
                // For this step (ViewModel), I will pass the Key. 
                // I suspect I need to check `ITranscriptionService` definition.
                
                var text = await _transcriptionService.TranscribeAsync(filePath, key, endpoint, model);
                
                if (_processingCts.Token.IsCancellationRequested) return;

                if (IsPostProcessingEnabled && SelectedRefinementPreset != null)
                {
                    Status = "Refining...";
                    
                    // Get profile from preset
                    var refProfile = Providers.FirstOrDefault(p => p.Id == SelectedRefinementPreset.ProfileId);
                    if (refProfile == null)
                    {
                        _logger.LogWarning("Preset has no valid profile, skipping refinement.");
                    }
                    else
                    {
                        _logger.LogInfo($"Refining with preset '{SelectedRefinementPreset.Name}' using {refProfile.Name}...");
                       
                        string rEndpoint = refProfile.BaseUrl.TrimEnd('/');
                        if (!rEndpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) 
                        {
                            rEndpoint += "/chat/completions";
                        }
                       
                        var refModel = !string.IsNullOrEmpty(SelectedRefinementPreset.Model) 
                            ? SelectedRefinementPreset.Model 
                            : refProfile.RefinementModel;
                        var prompt = SelectedRefinementPreset.SystemPrompt;
                       
                        text = await _textProcessor.ProcessTextAsync(text, prompt, refProfile.ApiKey, rEndpoint, refModel);
                    }
                }

                LastTranscription = text;
                await AddToHistory(text, filePath);
                Status = "Done";
                UiState = "Done";
                var processingElapsed = DateTime.Now - _processingStartTime;
                ProcessingTimeDisplay = $"{processingElapsed.TotalSeconds:F1}s".Replace(".", ",");

                await _inputInjector.TypeTextAsync(text, TextInjectionMode == 1);
                _logger.LogInfo("Text injected.");

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
        
        // ... (Remaining History methods are unchanged) ...

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
            // Update waves...
            // Simple random visualization based on volume
            if (_audioRecorder.IsRecording)
            {
                 System.Windows.Application.Current.Dispatcher.Invoke(() =>
                 {
                     var rand = new Random();
                     foreach (var bar in AudioWaves)
                     {
                         var targetHeight = (VolumeLevel * 300) * (rand.NextDouble() * 0.5 + 0.5); // Randomize slightly
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
            SaveSettings(); // explicit save on close
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
