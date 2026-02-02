namespace FluentDraft.Services.Interfaces
{
    public interface ISettingsService
    {
        AppSettings LoadSettings();
        void SaveSettings(AppSettings settings);
    }

    public class AppSettings
    {
        // Provider Profiles
        public List<FluentDraft.Models.ProviderProfile> Providers { get; set; } = new();

        // Selected Profile IDs
        public Guid? SelectedTranscriptionProfileId { get; set; }
        public Guid? SelectedRefinementProfileId { get; set; }  // Legacy, kept for migration
        
        // Refinement Presets
        public List<FluentDraft.Models.RefinementPreset> RefinementPresets { get; set; } = new();
        public Guid? SelectedRefinementPresetId { get; set; }
        
        // Post-processing settings
        public bool IsSetupCompleted { get; set; } = false;
        public bool IsPostProcessingEnabled { get; set; } = true;
        public bool PauseMediaOnRecording { get; set; } = false;
        public string PostProcessingPrompt { get; set; } = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided within [INPUT_TEXT] tags. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments."; // Legacy, kept for migration
        
        // General Settings
        public bool IsAlwaysOnTop { get; set; } = false;
        public List<int> HotkeyCodes { get; set; } = new List<int> { 0x14 }; // Default to CapsLock
        public int ActivationMode { get; set; } = 1; // 0 = TapToTalk, 1 = PushToTalk
        public bool PlaySoundOnRecord { get; set; } = true;
        public int SelectedMicrophone { get; set; } = 0; // Default to first available
        public int TextInjectionMode { get; set; } = 1; // 0 = Type, 1 = Paste
        public bool IsAutoInsertEnabled { get; set; } = true;
        public bool CloseToTray { get; set; } = true;
        public int MaxRecordingSeconds { get; set; } = 120; // Default 2 minutes
        public string? ChatSessionId { get; set; } // Generated User ID for session tracking

        // Legacy/Migration properties (Optional, but might help if we wanted to read old json seamlessly, 
        // but given we are rewriting the settings structure, we might manually migrate in ViewModel if we can read the old file, 
        // or just accept the reset. The plan mentioned migration logic in ViewModel.
        // For simplicity and cleanliness, we'll rely on a clean break or manual migration logic if the file is loaded with missing fields.)
    }
}
