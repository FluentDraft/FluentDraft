using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace FluentDraft.Models
{
    public partial class TranscriptionItem : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string? AudioFilePath { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ObservableProperty]
        private bool _isSelected;
    }
}
