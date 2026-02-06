using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using FluentDraft.Models;

namespace FluentDraft.Services.Interfaces
{
    public interface ILocalizationService : INotifyPropertyChanged
    {
        string CurrentLanguageCode { get; }
        List<LanguageModel> AvailableLanguages { get; }

        void SetLanguage(string code);
        string GetString(string key);
    }
}
