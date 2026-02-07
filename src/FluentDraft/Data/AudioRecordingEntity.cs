using System;
using System.ComponentModel.DataAnnotations;

namespace FluentDraft.Data
{
    public class AudioRecordingEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string FilePath { get; set; } = string.Empty; // Relative path in AppData or absolute? Prefer relative to Audio folder for portability? Or just absolute for simplicity. Plan says Move to AppData. Let's start with Filename and we construct path.
        public string FileName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TranscriptionText { get; set; } = string.Empty;
        public string? RawTranscription { get; set; } // Stores the initial transcription before refinement
        public string? TranscriptionModel { get; set; } // Stores the model used for transcription
        public string? RefinementPresetId { get; set; } // Stores the ID of the preset used for refinement
        public string? RefinementPresetName { get; set; } // Snapshot of preset name
        public bool IsProcessed { get; set; }
    }
}
