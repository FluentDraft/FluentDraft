using System.Threading.Tasks;

namespace FluentDraft.Services.Interfaces
{
    public interface ITextProcessorService
    {
        Task<string> ProcessTextAsync(string text, string prompt, string apiKey, string endpoint, string model);
        Task<IEnumerable<string>> GetAvailableModelsAsync(string apiKey, string baseUrl);
    }
}
