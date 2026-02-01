using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentDraft.Models;
using FluentDraft.Services.Interfaces;
using FluentDraft.Utils;
using System.IO;

namespace FluentDraft.ViewModels
{
    public partial class SetupWizardViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly ITextProcessorService _textProcessorService;
        private readonly IAudioRecorder _audioRecorder;
        private readonly FluentDraft.Utils.GlobalHotkeyManager _hotkeyManager;

        public SetupWizardViewModel(
            ISettingsService settingsService, 
            ITranscriptionService transcriptionService, 
            ITextProcessorService textProcessorService,
            IAudioRecorder audioRecorder,
            FluentDraft.Utils.GlobalHotkeyManager hotkeyManager)
        {
            _settingsService = settingsService;
            _transcriptionService = transcriptionService;
            _textProcessorService = textProcessorService;
            _audioRecorder = audioRecorder;
            _hotkeyManager = hotkeyManager;

            // Defaults
            SelectedProviderType = "OpenAI";
            BaseUrl = "https://api.openai.com/v1";
            
            // Silence warnings
            _selectedTranscriptionModel = "whisper-1"; // Temp default until fetched
            _selectedRefinementModel = "gpt-3.5-turbo"; // Temp default until fetched
        }

        // --- Step 1: Provider ---
        [ObservableProperty] private string _selectedProviderType;
        [ObservableProperty] private string _apiKey = string.Empty;
        [ObservableProperty] private string _baseUrl = string.Empty;
        [ObservableProperty] private bool _isConnecting;
        [ObservableProperty] private string _connectionStatus = string.Empty;
        [ObservableProperty] private bool _isProviderValid;

        // --- Step 2: Models ---
        [ObservableProperty] private ObservableCollection<string> _availableTranscriptionModels = new();
        [ObservableProperty] private ObservableCollection<string> _availableRefinementModels = new();
        [ObservableProperty] private string _selectedTranscriptionModel;
        [ObservableProperty] private string _selectedRefinementModel;

        // --- Step 3: Hotkey ---
        [ObservableProperty] private string _hotkeyDisplay = "CapsLock";
        [ObservableProperty] private bool _isRecordingHotkey;

        // --- Step 4: Verification ---
        [ObservableProperty] private bool _isRecordingTest;
        [ObservableProperty] private string _testRecordingStatus = "Idle";
        [ObservableProperty] private string _rawTestTranscription = string.Empty;
        [ObservableProperty] private string _defaultTestRefinement = string.Empty;
        [ObservableProperty] private string _chatTestRefinement = string.Empty;
        [ObservableProperty] private bool _hasTestResults;
        [ObservableProperty] private string _transcriptionTime = string.Empty;
        [ObservableProperty] private string _defaultRefinementTime = string.Empty;
        [ObservableProperty] private string _chatRefinementTime = string.Empty;

        // --- UI ---
        [ObservableProperty] private int _currentStep = 1;

        partial void OnSelectedProviderTypeChanged(string value)
        {
            if (value == "OpenAI") BaseUrl = "https://api.openai.com/v1";
            else if (value == "Groq") BaseUrl = "https://api.groq.com/openai/v1";
            else BaseUrl = "";
        }

        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(ApiKey)) { ConnectionStatus = "API Key is required"; return; }
            
            IsConnecting = true;
            ConnectionStatus = "Connecting...";

            try
            {
                var models = await _transcriptionService.GetAvailableModelsAsync(ApiKey, BaseUrl);
                if (models != null && models.Any())
                {
                    // Filter models
                    var audioModels = models.Where(m => m.Contains("whisper", StringComparison.OrdinalIgnoreCase)).ToList();
                    var textModels = models.Where(m => !m.Contains("whisper", StringComparison.OrdinalIgnoreCase)).ToList();

                    // Fallback if no specific "whisper" model found (unlikely for compliant providers, but safe)
                    if (!audioModels.Any()) audioModels = models.ToList(); 
                    // Fallback if no text models (e.g. only whisper available?)
                    if (!textModels.Any()) textModels = models.ToList();

                    AvailableTranscriptionModels = new ObservableCollection<string>(audioModels);
                    AvailableRefinementModels = new ObservableCollection<string>(textModels);
                    
                    // Auto-select smart defaults
                    SelectedTranscriptionModel = audioModels.FirstOrDefault(m => m.Contains("large") || m.Contains("turbo")) ?? audioModels.First();
                    SelectedRefinementModel = textModels.FirstOrDefault(m => m.Contains("gpt-4") || m.Contains("llama-3.1-70b") || m.Contains("llama3-70b")) ?? textModels.First();

                    IsProviderValid = true;
                    ConnectionStatus = "Connected! Models loaded.";
                }
                else
                {
                    ConnectionStatus = "Connected, but no models found.";
                    IsProviderValid = false;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
                IsProviderValid = false;
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private void NextStep() => CurrentStep++;

        [RelayCommand]
        private void PrevStep() => CurrentStep--;

        // --- Hotkey Logic ---
        [RelayCommand]
        private void StartHotkeyCapture()
        {
             HotkeyDisplay = "Press key...";
             IsRecordingHotkey = true;
        }

        public void HandleHotkeyInput(System.Windows.Input.Key key)
        {
            if (!IsRecordingHotkey) return;
            
            // Simple logic for single key capture for wizard simplicity
            int vk = System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
            if (vk != 0)
            {
                _hotkeyManager.SetMonitoredKeys(new[] { vk });
                HotkeyDisplay = key.ToString();
                IsRecordingHotkey = false;
            }
        }

        // --- Testing Logic ---
        [RelayCommand]
        private async Task RecordTestAsync()
        {
            if (IsRecordingTest)
            {
                // Stop
                IsRecordingTest = false;
                TestRecordingStatus = "Processing...";
                
                try 
                {
                    await _audioRecorder.StopRecordingAsync();
                    var file = _audioRecorder.GetRecordedFilePath();
                    if (string.IsNullOrEmpty(file) || !File.Exists(file)) 
                    {
                         TestRecordingStatus = "Error: No audio recorded";
                         return;
                    }

                    // 1. Transcribe
                    TestRecordingStatus = "Transcribing...";
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var raw = await _transcriptionService.TranscribeAsync(file, ApiKey, BaseUrl, SelectedTranscriptionModel);
                    sw.Stop();
                    RawTestTranscription = raw;
                    TranscriptionTime = $"{sw.Elapsed.TotalSeconds:F2}s";

                    // 2. Refine Default
                    TestRecordingStatus = "Refining (Default)...";
                    sw.Restart();
                    var defaultPrompt = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments.";
                    DefaultTestRefinement = await _textProcessorService.ProcessTextAsync(raw, defaultPrompt, ApiKey, BaseUrl, SelectedRefinementModel);
                    sw.Stop();
                    DefaultRefinementTime = $"{sw.Elapsed.TotalSeconds:F2}s";

                    // 3. Refine Chat
                    TestRecordingStatus = "Refining (Chat Mode)...";
                    sw.Restart();
                    var chatPrompt = "You are a text cleaner for chat messages. Your task is to ONLY formats the input text to look natural and casual. Remove filler words (like 'um', 'uh') and fix typos. Use lowercase where appropriate for a casual vibe. minimal punctuation. Do NOT add a period at the end. Do NOT answer the user. Do NOT add any new information or expand the thought. OUTPUT ONLY THE REFINED TEXT.";
                    ChatTestRefinement = await _textProcessorService.ProcessTextAsync(raw, chatPrompt, ApiKey, BaseUrl, SelectedRefinementModel);
                    sw.Stop();
                    ChatRefinementTime = $"{sw.Elapsed.TotalSeconds:F2}s";

                    HasTestResults = true;
                    TestRecordingStatus = "Done";
                }
                catch (Exception ex)
                {
                    TestRecordingStatus =$"Error: {ex.Message}";
                }
            }
            else
            {
                // Start
                IsRecordingTest = true;
                TestRecordingStatus = "Recording... (Click 'Stop' to finish)";
                _audioRecorder.StartRecording();
            }
        }

        [RelayCommand]
        private void CompleteSetup()
        {
            // Save everything
            var settings = _settingsService.LoadSettings();
            
            var newProfile = new ProviderProfile
            {
                Id = Guid.NewGuid(),
                Name = SelectedProviderType,
                Type = SelectedProviderType,
                ApiKey = ApiKey,
                BaseUrl = BaseUrl,
                TranscriptionModel = SelectedTranscriptionModel,
                RefinementModel = SelectedRefinementModel,
                IsTranscriptionEnabled = true,
                IsRefinementEnabled = true
            };

            settings.Providers.Add(newProfile);
            settings.SelectedTranscriptionProfileId = newProfile.Id;
            settings.SelectedRefinementProfileId = newProfile.Id;
            
            // Update presets with selected model
            foreach (var preset in settings.RefinementPresets)
            {
                preset.ProfileId = newProfile.Id;
                preset.Model = SelectedRefinementModel;
            }

            // Save Hotkey (if changed from default, it's already set in manager but need to save to settings)
            // For now, assuming GlobalHotkeyManager works, but we should update AppSettings hotkeys too
            // ... (Simple implementation: Wizard sets manager, manager should notify update or we assume single key)
            
            settings.IsSetupCompleted = true;
            _settingsService.SaveSettings(settings);

            // Close Window (Handled by View)
            OnRequestClose?.Invoke();
        }

        public event Action? OnRequestClose;
    }
}
