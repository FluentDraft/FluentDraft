using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class OpenAiCompatibleTranscriptionService : ITranscriptionService
    {
        protected readonly HttpClient _httpClient;
        protected readonly ISettingsService _settingsService;
        
        public OpenAiCompatibleTranscriptionService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
        }

        public async Task<string> TranscribeAsync(string filePath, string apiKey, string baseUrl, string modelName)
        {
            baseUrl = baseUrl?.TrimEnd('/') ?? string.Empty;
            
            if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentException("Transcription Base URL is not configured");
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentException($"API Key is required");
            if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found", filePath);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/audio/transcriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var content = new MultipartFormDataContent();
            
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            
            content.Add(fileContent, "file", Path.GetFileName(filePath));
            content.Add(new StringContent(modelName), "model");
            content.Add(new StringContent("json"), "response_format");

            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Transcription API Error: {response.StatusCode} - {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
        }

        public async Task<List<string>> GetAvailableModelsAsync(string apiKey, string baseUrl)
        {
                if (string.IsNullOrEmpty(apiKey)) throw new ArgumentException("API Key is required");
                if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentException("Base URL is required");

                baseUrl = baseUrl.TrimEnd('/');

                // Some providers might have different endpoints for models, but standard OpenAI is /models
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API Error: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                
                JsonDocument doc;
                try 
                {
                    doc = JsonDocument.Parse(json);
                }
                catch (JsonException)
                {
                    // Likely HTML error page or non-JSON response
                    throw new Exception($"Invalid API response (not JSON): {json.Substring(0, Math.Min(100, json.Length))}...");
                }
                
                var models = new List<string>();
                using (doc)
                {
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in data.EnumerateArray())
                        {
                            if (element.TryGetProperty("id", out var idProp))
                            {
                                var id = idProp.GetString();
                                // Filter for whisper models generally, but allow others if user wants custom
                                if (!string.IsNullOrEmpty(id))
                                {
                                    models.Add(id);
                                }
                            }
                        }
                    }
                }
                
                return models;
        }
    }
}
