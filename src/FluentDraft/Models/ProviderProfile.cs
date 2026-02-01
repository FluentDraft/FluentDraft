using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace FluentDraft.Models
{
    public partial class ProviderProfile : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _name = "New Provider";

        [ObservableProperty]
        private string _type = "Groq"; // Groq, OpenAI, Custom

        [ObservableProperty]
        private string _baseUrl = "";

        [ObservableProperty]
        private string _apiKey = "";

        [ObservableProperty]
        private string _transcriptionModel = ""; 

        [ObservableProperty]
        private string _refinementModel = ""; 
        
        // Capability flags
        [ObservableProperty]
        private bool _isTranscriptionEnabled = true;

        [ObservableProperty]
        private bool _isRefinementEnabled = true;

        [ObservableProperty]
        private bool _isValidated = false;

        [ObservableProperty]
        private ObservableCollection<string> _transcriptionModels = new();

        [ObservableProperty]
        private ObservableCollection<string> _refinementModels = new();

        public override string ToString()
        {
            return Name;
        }

        partial void OnTypeChanged(string value)
        {
            if (value == "Groq") 
            {
                BaseUrl = "https://api.groq.com/openai/v1";
                TranscriptionModel = "whisper-large-v3";
                RefinementModel = "llama3-70b-8192";
            }
            else if (value == "OpenAI") 
            {
                BaseUrl = "https://api.openai.com/v1";
                TranscriptionModel = "whisper-1";
                RefinementModel = "gpt-4o";
            }
            else if (value == "Custom") 
            {
                BaseUrl = "";
                TranscriptionModel = "";
                RefinementModel = "";
            }
            
            IsValidated = false;
        }
        
        partial void OnBaseUrlChanged(string value) => IsValidated = false;
        partial void OnApiKeyChanged(string value) => IsValidated = false;
        partial void OnTranscriptionModelChanged(string value) => IsValidated = false;
        partial void OnRefinementModelChanged(string value) => IsValidated = false;
    }
}
