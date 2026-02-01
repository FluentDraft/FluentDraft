using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentDraft.Models
{
    public partial class RefinementPreset : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _name = "New Preset";

        [ObservableProperty]
        private Guid? _profileId;  // Reference to ProviderProfile

        [ObservableProperty]
        private string _model = "";

        [ObservableProperty]
        private string _systemPrompt = "You are a text refinement assistant. Your goal is to correct grammar, add punctuation, and improve clarity of the text provided. Maintain the original meaning and tone. Output ONLY the refined text itself, without any tags or additional comments.";

        // Available models fetched from the selected profile
        [ObservableProperty]
        private ObservableCollection<string> _availableModels = new();

        public override string ToString()
        {
            return Name;
        }
    }
}
