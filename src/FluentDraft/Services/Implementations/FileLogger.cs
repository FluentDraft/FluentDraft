using System;
using System.IO;
using System.Text;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class FileLogger : ILoggingService
    {
        private readonly string _logDir;
        private readonly string _logFile;
        private readonly StringBuilder _inMemoryLogs = new StringBuilder();
        private bool _isDebugModeEnabled = false;

        public event Action<string>? OnLogAdded;

        public FileLogger()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDir = Path.Combine(appData, "FluentDraft", "logs");
            
            if (!Directory.Exists(_logDir))
            {
                Directory.CreateDirectory(_logDir);
            }

            _logFile = Path.Combine(_logDir, $"log_{DateTime.Now:yyyyMMdd}.txt");
            LogInfo("Logger initialized.");
        }

        public void SetDebugMode(bool enabled)
        {
            _isDebugModeEnabled = enabled;
            if (enabled) LogInfo("Debug Mode ENABLED.");
        }

        public void LogInfo(string message) => Log("INFO", message);
        public void LogWarning(string message) => Log("WARN", message);
        public void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message} | Exception: {ex.Message} | StackTrace: {ex.StackTrace}" : message;
            Log("ERROR", fullMessage);
        }

        public string GetLogContent() => _inMemoryLogs.ToString();

        private void Log(string level, string message)
        {
            var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            
            _inMemoryLogs.AppendLine(formattedMessage);
            OnLogAdded?.Invoke(formattedMessage);

            // In Debug Mode: Write ALL logs to file
            // Normal Mode: Only write ERROR calls to file
            bool shouldWriteToFile = _isDebugModeEnabled || level == "ERROR";

            if (shouldWriteToFile)
            {
                try
                {
                    File.AppendAllLines(_logFile, new[] { formattedMessage });
                }
                catch
                {
                    // If file is locked, we just skip writing to disk but keep in memory
                }
            }
        }
    }
}
