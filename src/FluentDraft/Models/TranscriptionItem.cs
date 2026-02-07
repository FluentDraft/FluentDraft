using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace FluentDraft.Models
{
    public partial class TranscriptionItem : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowRawText))]
        private string _text = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowRawText))]
        private string? _rawText; 

        public string? TranscriptionModel { get; set; }
        public string? RefinementPresetId { get; set; }
        public string? RefinementPresetName { get; set; }
        public string? AudioFilePath { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isExpanded;

        public bool ShowRawText => !string.IsNullOrEmpty(RawText) && RawText != Text;
    }
}
