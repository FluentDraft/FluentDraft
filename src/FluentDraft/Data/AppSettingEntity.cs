using System;
using System.ComponentModel.DataAnnotations;

namespace FluentDraft.Data
{
    public class AppSettingEntity
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
