namespace FluentDraft.Models
{
    public class LanguageModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; // Native name (e.g. "Deutsch")
        public string NativeName { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
}
