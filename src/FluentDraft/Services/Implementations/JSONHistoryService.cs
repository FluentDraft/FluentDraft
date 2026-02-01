using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentDraft.Models;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class JSONHistoryService : IHistoryService
    {
        private readonly string _filePath;
        private List<TranscriptionItem> _cache;

        public JSONHistoryService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "FluentDraft");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "history.json");
            _cache = new List<TranscriptionItem>();
        }

        public async Task<List<TranscriptionItem>> GetHistoryAsync()
        {
            if (_cache.Count > 0) return _cache;

            if (!File.Exists(_filePath)) return new List<TranscriptionItem>();

            try
            {
                using var stream = File.OpenRead(_filePath);
                _cache = await JsonSerializer.DeserializeAsync<List<TranscriptionItem>>(stream) ?? new List<TranscriptionItem>();
            }
            catch
            {
                _cache = new List<TranscriptionItem>();
            }

            return _cache;
        }

        public async Task AddAsync(TranscriptionItem item)
        {
            await GetHistoryAsync(); // Ensure loaded
            _cache.Insert(0, item);
            await SaveAsync();
        }

        public async Task DeleteAsync(TranscriptionItem item)
        {
            await GetHistoryAsync();
            var toRemove = _cache.Find(x => x.Id == item.Id);
            if (toRemove != null)
            {
                _cache.Remove(toRemove);
                // Optionally delete audio file? User didn't specify, but safer to keep or ask. For now, just remove record.
                await SaveAsync();
            }
        }

        public async Task ClearAsync()
        {
            _cache.Clear();
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, _cache);
        }
    }
}
