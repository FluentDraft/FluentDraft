using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentDraft.Models;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public partial class LocalizationService : ObservableObject, ILocalizationService
    {
        private readonly ISettingsService _settingsService;
        
        [ObservableProperty]
        private string _currentLanguageCode = "en";

        public List<LanguageModel> AvailableLanguages { get; } = new List<LanguageModel>
        {
            new LanguageModel { Code = "en", Name = "English" },
            new LanguageModel { Code = "de", Name = "Deutsch" },
            new LanguageModel { Code = "es", Name = "Español" },
            new LanguageModel { Code = "fr", Name = "Français" },
            new LanguageModel { Code = "it", Name = "Italiano" },
            new LanguageModel { Code = "ru", Name = "Русский" }
        };

        public LocalizationService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();
            SetLanguage(settings.LanguageCode);
        }

        public async void SetLanguage(string code)
        {
            if (string.IsNullOrEmpty(code)) code = "en";
            
            var lang = AvailableLanguages.FirstOrDefault(l => l.Code == code);
            if (lang == null)
            {
                code = "en";
            }

            // Load ResourceDictionary
            var dict = new ResourceDictionary();
            string source = $"pack://application:,,,/FluentDraft;component/Resources/Languages/Strings.{code}.xaml";
            
            // Fallback to English if file not found (though simple string concat implies we expect it)
            if (code == "en")
            {
                source = "pack://application:,,,/FluentDraft;component/Resources/Languages/Strings.xaml";
            }
            
            try 
            {
                dict.Source = new Uri(source);
                
                var appResources = Application.Current.Resources;
                var oldDict = appResources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Strings."));
                
                if (oldDict != null)
                {
                    appResources.MergedDictionaries.Remove(oldDict);
                }
                
                appResources.MergedDictionaries.Add(dict);
                
                CurrentLanguageCode = code;
                
                // Persist if changed
                var settings = await _settingsService.LoadSettingsAsync();
                if (settings.LanguageCode != code)
                {
                    settings.LanguageCode = code;
                    await _settingsService.SaveSettingsAsync(settings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocService] Failed to load language {code}: {ex}");
                if (code != "en") SetLanguage("en");
            }
        }

        public string GetString(string key)
        {
            if (Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key] as string ?? key;
            }
            return key;
        }
    }
}
