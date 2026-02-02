using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly AudioDeviceService _audioDeviceService;
        private readonly GlobalHotkeyManager _hotkeyManager;
        private readonly ILoggingService _logger;
        private readonly IUpdateService _updateService;

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
        public RelayCommand<RefinementPreset> ResetPresetCommand { get; }
        public RelayCommand<RefinementPreset> FetchPresetModelsCommand { get; }

        // General Settings
        [ObservableProperty]
        private bool _isAlwaysOnTop = false;

        [ObservableProperty]
        private bool _isPostProcessingEnabled = true;

        [ObservableProperty]
        private bool _pauseMediaOnRecording = false;

        [ObservableProperty]
        private string _hotkeyDisplay = "CapsLock";

        [ObservableProperty]
        private bool _isRecordingHotkey = false;

        [ObservableProperty]
        private ObservableCollection<AudioDeviceModel> _audioDevices = new();

        [ObservableProperty]
        private int _selectedAudioDevice = 0;

        [ObservableProperty]
        private int _activationMode = 1; // 0 = TapToTalk, 1 = PushToTalk

        [ObservableProperty]
        private int _textInjectionMode = 1; // 0 = Type, 1 = Paste

        [ObservableProperty]
        private bool _playSoundOnRecord = true;

        [ObservableProperty]
        private bool _closeToTray = true;

        [ObservableProperty]
        private int _maxRecordingSeconds = 120;

        // Update Properties
        [ObservableProperty]
        private string _updateStatus = "Check for updates";

        [ObservableProperty]
        private bool _isCheckingForUpdates = false;

        [ObservableProperty]
        private bool _isUpdateReady = false;

        [ObservableProperty]
        private int _updateDownloadProgress = 0;

        private UpdateInfo? _pendingUpdate;

        // About Properties
        public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        
        public ObservableCollection<string> AvailableProviderTypes { get; } = new() { "Groq", "OpenAI", "Custom" };

        private List<int> _currentHotkeyCodes = new List<int> { 0x14 };

        public RelayCommand StartRecordingHotkeyCommand { get; }
        public RelayCommand CloseSettingsCommand { get; }
        public RelayCommand CheckForUpdatesCommand { get; }
        public RelayCommand ApplyUpdateCommand { get; }

        public SettingsViewModel(
            ISettingsService settingsService,
            ITranscriptionService transcriptionService,
            AudioDeviceService audioDeviceService,
            GlobalHotkeyManager hotkeyManager,
            ILoggingService logger,
            IUpdateService updateService)
        {
            _settingsService = settingsService;
            _transcriptionService = transcriptionService;
            _audioDeviceService = audioDeviceService;
            _hotkeyManager = hotkeyManager;
            _logger = logger;
            _updateService = updateService;

            LoadAudioDevices();

            AddProviderCommand = new RelayCommand(AddProvider);
            RemoveProviderCommand = new RelayCommand<ProviderProfile>(RemoveProvider);
            TestProviderCommand = new RelayCommand<ProviderProfile>(async (p) => await TestProvider(p));
            FetchModelsCommand = new RelayCommand<ProviderProfile>(async (p) => await FetchModels(p));
            
            AddPresetCommand = new RelayCommand(AddPreset);
            RemovePresetCommand = new RelayCommand<RefinementPreset>(RemovePreset);
            ResetPresetCommand = new RelayCommand<RefinementPreset>(ResetPreset);
            FetchPresetModelsCommand = new RelayCommand<RefinementPreset>(async (p) => { if (p != null) await FetchPresetModelsAsync(p); });

            StartRecordingHotkeyCommand = new RelayCommand(StartNewHotkeyCapture);
            CloseSettingsCommand = new RelayCommand(RequestCloseSettings);
            CheckForUpdatesCommand = new RelayCommand(async () => await CheckForUpdates());
            ApplyUpdateCommand = new RelayCommand(ApplyUpdate);

            WeakReferenceMessenger.Default.Register<UpdateReadyMessage>(this, (r, m) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    _pendingUpdate = m.Update;
                    IsUpdateReady = true;
                    UpdateStatus = $"Update v{m.Update.TargetFullRelease.Version} is ready!";
                });
            });

            LoadSettings();

            // Hotkey listening logic needs to pass key events here or handle centrally
            // Ideally MainWindow handles PreviewKeyDown and calls ViewModel.HandleHotkeyInput
        }

        private void RequestCloseSettings()
        {
            if (!ValidateSettings()) return;
            SaveSettings();
            
            // Close window
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is FluentDraft.Views.SettingsWindow)
                {
                    window.Close();
                    return;
                }
            }
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

        private void AddProvider()
        {
            var newProfile = new ProviderProfile { Name = "New Provider", Type = "Groq", IsTranscriptionEnabled=true, IsRefinementEnabled=true };
            newProfile.PropertyChanged += OnItemChanged;
            Providers.Add(newProfile);
            SelectedEditingProvider = newProfile; // Auto-select for editing
            SaveSettings(); // Auto-save on add? Or wait? MainViewModel did save.
            RefreshFilteredCollections();
        }

        private void RemoveProvider(ProviderProfile? profile)
        {
            if (profile == null) return;
            profile.PropertyChanged -= OnItemChanged;
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
            newPreset.PropertyChanged += OnItemChanged;
            RefinementPresets.Add(newPreset);
            SelectedEditingPreset = newPreset;
            SaveSettings();
        }

        private void RemovePreset(RefinementPreset? preset)
        {
            if (preset == null || RefinementPresets.Count <= 1) return; // Keep at least one preset
            preset.PropertyChanged -= OnItemChanged;
            RefinementPresets.Remove(preset);
            if (SelectedEditingPreset == preset) SelectedEditingPreset = null;
            if (SelectedRefinementPreset == preset) SelectedRefinementPreset = RefinementPresets.FirstOrDefault();
            SaveSettings();
        }

        private void ResetPreset(RefinementPreset? preset)
        {
            if (preset == null) return;
            
            if (System.Windows.MessageBox.Show($"Are you sure you want to reset '{preset.Name}' to default values?", "Confirm Reset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
            {
                // Reset to default system prompt
                preset.SystemPrompt = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments.";
                
                // Also reset model to profile default if possible
                var profile = Providers.FirstOrDefault(p => p.Id == preset.ProfileId);
                if (profile != null && !string.IsNullOrEmpty(profile.RefinementModel))
                {
                    preset.Model = profile.RefinementModel;
                }

                SaveSettings();
            }
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
            
            // Preserve selection
            if (!string.IsNullOrEmpty(currentTrans) && profile.TranscriptionModels.Contains(currentTrans)) profile.TranscriptionModel = currentTrans;
            if (!string.IsNullOrEmpty(currentRef) && profile.RefinementModels.Contains(currentRef)) profile.RefinementModel = currentRef;
        }

        private void RefreshFilteredCollections()
        {
            OnPropertyChanged(nameof(AvailableTranscriptionProfiles));
            OnPropertyChanged(nameof(AvailableRefinementProfiles));
        }

        private void LoadSettings()
        {
            var settings = _settingsService.LoadSettings();
            
            // Note: Migration logic is implicitly handled if we load the same way.
            // Simplified here: assuming settings are already valid or we default them.
            // Ideally we duplicate the migration logic or have a shared Migrator, but for now copying is safer.

            if (settings.Providers == null || !settings.Providers.Any())
            {
                // Init defaults (simplified)
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

            if (settings.RefinementPresets == null || !settings.RefinementPresets.Any())
            {
                // Init defaults (simplified - assuming clean state, but ideally we should keep logic consistent)
                 var defaultProfile = Providers.FirstOrDefault(p => p.IsRefinementEnabled);
                 // ... defaulting logic omitted for brevity, assuming loaded settings are generally OK or handled by MainViewModel before
                 // BUT if we open settings first, we need this.
                 // Let's just create one default if empty
                 if (settings.RefinementPresets == null) settings.RefinementPresets = new List<RefinementPreset>();
            if (settings.RefinementPresets.Count == 0)
                 {
                    settings.RefinementPresets.Add(new RefinementPreset { Name="Default", ProfileId = defaultProfile?.Id });
                 }
            }
            
            RefinementPresets = new ObservableCollection<RefinementPreset>(settings.RefinementPresets);
            SelectedRefinementPreset = RefinementPresets.FirstOrDefault(p => p.Id == settings.SelectedRefinementPresetId) ?? RefinementPresets.FirstOrDefault();

            // Subscribe to changes
            foreach(var p in Providers) p.PropertyChanged += OnItemChanged;
            foreach(var r in RefinementPresets) r.PropertyChanged += OnItemChanged;

            IsAlwaysOnTop = settings.IsAlwaysOnTop;
            CloseToTray = settings.CloseToTray;
            IsPostProcessingEnabled = settings.IsPostProcessingEnabled;
            PlaySoundOnRecord = settings.PlaySoundOnRecord;
            PauseMediaOnRecording = settings.PauseMediaOnRecording;
            ActivationMode = settings.ActivationMode;
            TextInjectionMode = settings.TextInjectionMode;
            SelectedAudioDevice = settings.SelectedMicrophone;
            MaxRecordingSeconds = settings.MaxRecordingSeconds;

            _currentHotkeyCodes = settings.HotkeyCodes ?? new List<int> { 0x14 };
            UpdateHotkeyDisplay();
            
            RefreshFilteredCollections();
        }

        private void OnItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveSettings();
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
            
            // Notify other components
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage());
            
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
                // SaveSettings(); // Do we save immediately on change? MainViewModel did.
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
                _currentHotkeyCodes.Add(0x14); 
            }

            var names = _currentHotkeyCodes.Select(vk => ((Key)KeyInterop.KeyFromVirtualKey(vk)).ToString());
            HotkeyDisplay = string.Join(" + ", names).Replace("Capital", "CapsLock");
        }

        private async Task FetchPresetModelsAsync(RefinementPreset preset)
        {
            var profile = Providers.FirstOrDefault(p => p.Id == preset.ProfileId);
            if (profile == null) return;
            
            var currentModel = preset.Model;
            
            try
            {
                _logger.LogInfo($"Fetching text models for preset '{preset.Name}'...");
                var allModels = await _transcriptionService.GetAvailableModelsAsync(profile.ApiKey, profile.BaseUrl);
                
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
                    var newModels = new List<string>(textModels);
                    if (!string.IsNullOrEmpty(currentModel) && !newModels.Contains(currentModel))
                    {
                        newModels.Insert(0, currentModel);
                    }
                    
                    preset.AvailableModels.Clear();
                    foreach (var model in newModels) preset.AvailableModels.Add(model);
                    
                    if (!string.IsNullOrEmpty(currentModel)) preset.Model = currentModel;
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
                IsUpdateReady = false;
                UpdateDownloadProgress = 0;
                UpdateStatus = "Checking for updates...";
                
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
                     await _updateService.DownloadUpdateAsync(updateInfo, (p) => 
                     {
                         System.Windows.Application.Current.Dispatcher.Invoke(() => 
                         {
                             UpdateDownloadProgress = p;
                             UpdateStatus = $"Downloading: {p}%";
                         });
                     });
                     
                     _pendingUpdate = updateInfo;
                     IsUpdateReady = true;
                     UpdateStatus = $"Version {updateInfo.TargetFullRelease.Version} ready to install!";
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

        private void ApplyUpdate()
        {
            if (_pendingUpdate == null) return;
            _updateService.ApplyUpdateAndRestart(_pendingUpdate);
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

        // Property Change Handlers to trigger saves
        partial void OnIsAlwaysOnTopChanged(bool value) => SaveSettings();
        partial void OnIsPostProcessingEnabledChanged(bool value) => SaveSettings();
        partial void OnActivationModeChanged(int value) => SaveSettings();
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
                // _ = FetchPresetModelsAsync(value); // Auto-fetch removed to prevent overwrite/errors
            }
        }
    }
}
