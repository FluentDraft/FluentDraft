# FluentDraft Project Memory

This file serves as persistent memory for AI agents working on FluentDraft. It summarizes architecture, key decisions, and "hidden" knowledge to ensure consistency across sessions.

## 🏗️ Architecture Summary

- **Framework**: WPF (.NET 10)
- **Pattern**: MVVM (using `CommunityToolkit.Mvvm`)
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection` (configured in `App.xaml.cs`).
- **Update System**: Velopack (GitHub Releases).

## 🌍 Localization System

- **Logic**: `LocalizationService.cs` handles dynamic loading of `ResourceDictionary`.
- **Files**: Located in `src/FluentDraft/Resources/Languages/`.
- **Convention**:
  - `Strings.xaml` (Default/English)
  - `Strings.ru.xaml` (Russian), etc.
- **Key Note**: When adding UI strings, they MUST be added to ALL language files to avoid "Blank Text" bugs.
- **Binding**: Use `SelectedLanguage` in `SettingsViewModel` to trigger updates.

## 🐞 Logging & Debugging

- **Service**: `FileLogger.cs` (implements `ILoggingService`).
- **Debug Mode**: Toggleable in Settings. When OFF, only `ERROR` level logs are written to disk. When ON, all levels are logged.
- **Log Path**: `AppData/FluentDraft/logs/`.

## 🎙️ Audio & Voice Typing

- **Capture**: `NAudio` for recording.
- **Processing**: `OpenAiCompatibleTranscriptionService` for speech-to-text.
- **Injection**: `WindowsInputInjector` for typing/pasting text into other apps.
- **VAD**: `SileroVAD` for voice activity detection (tuning is critical to avoid cut-offs).

## 🔑 Key Components

- `MainViewModel`: Orchestrates the recording/processing flow.
- `SettingsViewModel`: Manages all persistence and provider configurations.
- `UniversalTextProcessor`: The logic layer that cleans and refines AI output.

## 🛠️ Recurring Tasks

- **Release**: Use the `/release` workflow.
- **Commit Style**: Use conventional commits (feat, fix, chore).
- **Versioning**: Checked in `FluentDraft.csproj` as `<Version>`.
