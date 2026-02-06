using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class UniversalTextProcessor : ITextProcessorService
    {
        private readonly HttpClient _httpClient;

        public UniversalTextProcessor()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> ProcessTextAsync(string text, string prompt, string apiKey, string endpoint, string model, string? chatSessionId = null)
        {
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentException("API Key is required for text processing");
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentException("Endpoint URL is required");
            if (string.IsNullOrEmpty(model)) throw new ArgumentException("Model name is required");

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = text }
                },
                temperature = 0.1,
                max_tokens = 1000,
                user = chatSessionId // Optional user ID for session tracking
            };

            // Construct the full URL. If it already looks like a chat completion endpoint, use it.
            // Otherwise, assume it's a base URL and append /chat/completions.
            var requestUrl = endpoint.TrimEnd('/');
            if (!requestUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                requestUrl += "/chat/completions";
            }

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"AI Text Processor Error ({response.StatusCode}): {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim() ?? string.Empty;

            // Remove <think>...</think> blocks common in reasoning models (DeepSeek, Qwen)
            content = System.Text.RegularExpressions.Regex.Replace(content, @"<think>.*?</think>", "", System.Text.RegularExpressions.RegexOptions.Singleline).Trim();

            return content;
        }

        public async Task<System.Collections.Generic.IEnumerable<string>> GetAvailableModelsAsync(string apiKey, string baseUrl)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(baseUrl))
                return new System.Collections.Generic.List<string>();

            // Adjust base URL to point to /models if it points to /chat/completions
            var modelsUrl = baseUrl;
            if (modelsUrl.EndsWith("/chat/completions"))
            {
                modelsUrl = modelsUrl.Replace("/chat/completions", "/models");
            }
            else if (!modelsUrl.EndsWith("/models"))
            {
                // Simple heuristic: if it doesn't end in /models, append it. 
                // Careful with trailing slashes.
                modelsUrl = modelsUrl.TrimEnd('/') + "/models";
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return new System.Collections.Generic.List<string>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var models = new System.Collections.Generic.List<string>();
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var element in data.EnumerateArray())
                    {
                        if (element.TryGetProperty("id", out var id))
                        {
                            models.Add(id.GetString() ?? "");
                        }
                    }
                }

                return models;
            }
            catch
            {
                return new System.Collections.Generic.List<string>();
            }
        }
    }
}
