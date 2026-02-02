using System;
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
            var settings = _settingsService.LoadSettings();
            SetLanguage(settings.LanguageCode);
        }

        public void SetLanguage(string code)
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
                System.Diagnostics.Debug.WriteLine($"[LocService] Loading: {source}");
                dict.Source = new Uri(source);
                
                var appResources = Application.Current.Resources;
                var oldDict = appResources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Strings."));
                
                if (oldDict != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocService] Removing old: {oldDict.Source}");
                    appResources.MergedDictionaries.Remove(oldDict);
                }
                
                appResources.MergedDictionaries.Add(dict);
                System.Diagnostics.Debug.WriteLine($"[LocService] Added new dictionary. Count: {appResources.MergedDictionaries.Count}");
                
                CurrentLanguageCode = code;
                
                // Persist if changed
                var settings = _settingsService.LoadSettings();
                if (settings.LanguageCode != code)
                {
                    settings.LanguageCode = code;
                    _settingsService.SaveSettings(settings);
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
