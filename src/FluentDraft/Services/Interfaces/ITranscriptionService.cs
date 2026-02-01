using System.Threading.Tasks;

namespace FluentDraft.Services.Interfaces
{
    public interface ITranscriptionService
    {
        Task<string> TranscribeAsync(string filePath, string apiKey, string baseUrl, string modelName);
        Task<System.Collections.Generic.List<string>> GetAvailableModelsAsync(string apiKey, string baseUrl);
        // Properties removed as service should be stateless regarding config
    }
}
