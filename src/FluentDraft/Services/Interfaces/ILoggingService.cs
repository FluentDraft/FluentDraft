using System;

namespace FluentDraft.Services.Interfaces
{
    public interface ILoggingService
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
        string GetLogContent();
        event Action<string>? OnLogAdded;
    }
}
