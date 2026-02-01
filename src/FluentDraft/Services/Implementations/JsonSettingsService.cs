using System;
using System.IO;
using System.Text.Json;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly string _settingsFile;
        private readonly ILoggingService _logger;

        public JsonSettingsService(ILoggingService logger)
        {
            _logger = logger;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "FluentDraft");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _settingsFile = Path.Combine(folder, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading settings", ex);
            }

            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
                _logger.LogInfo("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving settings", ex);
            }
        }
    }
}
