# Changelog

## [1.8.0] - 2026-02-07

### ✨ What's New
- **Immutable History**: Every refinement now creates a new entry in your history instead of overwriting the previous one. This lets you compare versions and keep your original raw transcriptions safe.
- **Refinement Context**: The history list now shows which Preset (e.g., "Summarize", "Fix Grammar") was used for each item, making it easier to track your workflow.
- **Visual Processing Feedback**: A clear "Processing" overlay now appears when refining text, preventing confusion with the previous result.

### ⚡ Improvements
- **Build System**: Fixed several internal build issues to ensure smoother future updates.
- **Performance**: Optimized how history items are added to the list for better responsiveness.

## [1.7.5] - 2026-02-07

### 🐛 Bug Fixes
- **Stability**: Fixed a resource leak that could cause the application to consume more memory over time.

### ⚡ Improvements
- **Security Policy**: Updated security and vulnerability reporting guidelines.

## [1.7.4] - 2026-02-06
### ✨ What's New
- **Native Fluent Splash Screen**: Added a beautiful, fast-loading splash screen that matches the modern Fluent design of Windows. It provides immediate feedback when you launch the app, making the startup experience feel smoother and more polished.

## [1.7.3] - 2026-02-06
### 🐛 Bug Fixes
- **Installer**: Ensure sound files are correctly tracked in git and included in the release.

## [1.7.2] - 2026-02-06
### 🐛 Bug Fixes
- **Installer**: Fixed an issue where sound files were filtered out of the final installer package.

## [1.7.1] - 2026-02-06

### 🐛 Bug Fixes
- **Sound Files**: Fixed an issue where sound files (start/stop) were missing from the installation and build output.

## [1.7.0] - 2026-02-06

### ✨ What's New
- **Data Persistence**: We've completely overhauled how the app stores data. Your settings and recordings are now saved in a secure system folder, ensuring they are never lost when you update the application.
- **Enhanced Security**: API keys are now fully encrypted on your device, adding an extra layer of protection for your credentials.
- **Automatic Migration**: The app will automatically move your existing settings and recordings to the new secure location when you first launch this version.

### ⚡ Improvements
- Improved application stability and data loading performance.

## [1.6.0] - 2026-02-03

### ✨ What's New
- **Dynamic Tray Icon**: The system tray icon now changes color (Green/Red) to clearly show when the application is recording your voice.
- **Enhanced Dark Context Menu**: Updated the system tray menu with a custom dark theme that better matches the Windows styling.
- **Optimized History View**: The History tab now features a more compact design that automatically adjusts to your window size, making it easier to manage your transcriptions.
- **Security Policy**: Added a formal security policy and vulnerability reporting guidelines to ensure your data stays protected.

### ⚡ Improvements
- Simplified the system tray interaction for faster access to recording status.
- Improved UI responsiveness when resizing the History view.

## [1.5.0] - 2026-02-03

### ✨ What's New
- **Multi-Language Support**: FluentDraft is now available in **English, Russian, German, Spanish, French, and Italian**! The interface language can be changed instantly in Settings.
- **Debug Mode**: Added a new troubleshooting option in Settings > System. When enabled, the app logs detailed information to a file, making it easier to diagnose issues.

### 🐛 Bug Fixes
- Fixed a startup crash caused by incorrect resource loading.
- Resolved an issue where some buttons or labels appeared with blank text.
- Fixed a bug where changing the language setting didn't update the UI immediately.

### ⚡ Improvements
- Optimized resource handling for faster application startup.
- Improved Settings window layout for better localization support.

## [1.4.0] - 2026-02-02

### ✨ What's New
- **Recording Cancellation**: You can now press "Stop" or the "Esc" key to instantly discard a recording without processing it.
- **New Settings Interface**: Completely redesigned settings window with tabs for Speech, Refinement, Services, and System for easier navigation.
- **Improved API Key Security**: API keys are now masked (like passwords) and include a convenient "Paste" button.
- **Helpful Tips**: Added explanations to settings options to help you get set up faster.

### ⚡ Improvements
- The "Get API Key" link now intelligently directs you to the correct provider's page (Groq or OpenAI).

## [1.3.2] - 2026-02-02

### ✨ What's New
- **Preset Reset**: Added a "Reset to Defaults" button for individual refinement presets to easily restore original settings.
- **Update Notifications**: The app now shows progress when downloading updates and asks for confirmation before restarting.

## [1.3.1] - 2026-02-01

### 🐛 Bug Fixes
- Fixed an issue where changing the model for one preset would incorrectly clear models for others.

## [1.3.0] - 2026-02-01

### ✨ What's New
- **Recording Time Limit**: Recordings successfully auto-stop after 2 minutes to ensure reliable transcription.

## [1.2.0] - 2026-01-31

### ✨ What's New
- **Sound Effects**: Added optional sound cues for start and stop recording.
- **Refined Output**: AI models now return cleaner text without "thinking" blocks.

## [1.1.0] - 2026-01-30

### ✨ What's New
- **Universal Provider Support**: Connect to any OpenAI-compatible service (like LocalAI or Ollama).

## [1.0.0] - 2026-01-26

### ✨ What's New
- Initial release of FluentDraft.
