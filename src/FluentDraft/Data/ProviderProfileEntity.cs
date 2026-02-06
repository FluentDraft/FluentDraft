using System;
using System.ComponentModel.DataAnnotations;

namespace FluentDraft.Data
{
    public class ProviderProfileEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string EncryptedApiKey { get; set; } = string.Empty; // Store encrypted
        public string TranscriptionModel { get; set; } = string.Empty;
        public string RefinementModel { get; set; } = string.Empty;
        public bool IsTranscriptionEnabled { get; set; }
        public bool IsRefinementEnabled { get; set; }
    }
}
