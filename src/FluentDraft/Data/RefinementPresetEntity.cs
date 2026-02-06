using System;
using System.ComponentModel.DataAnnotations;

namespace FluentDraft.Data
{
    public class RefinementPresetEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ProfileId { get; set; }
        public string Model { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
    }
}
