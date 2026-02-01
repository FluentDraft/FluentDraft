# Changelog

All notable changes to FluentDraft will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2026-02-01

### ✨ New Features
- Add **Velopack auto-update system** - app now checks for updates on startup
- Add `UpdateService` for managing application updates from GitHub Releases
- New GitHub Actions workflow with Velopack CLI for creating installers

### 🐛 Bug Fixes
- Fix Setup Wizard showing on every launch even when providers are configured
- Prevent duplicate providers from being created in Setup Wizard
- Smart detection of existing providers by API key and Base URL

### 🔧 Improvements
- Settings now properly persist between app updates (`%APPDATA%/FluentDraft/`)
- Auto-fix `IsSetupCompleted` flag if valid providers exist

## [1.1.0] - 2026-02-01


### ✨ New Features
- Add GitHub Actions workflow for automated builds and releases (#af879b4)
- Add CI/CD pipeline with automatic release on tag push (#d32e769)
- Update README with Quick Start guide and user-friendly documentation (#d32e769)

### 🐛 Bug Fixes
- Fix preset model selection being lost when switching between presets (#5ed772a)
- Always show presets list even when AI Refinement is disabled (#5ed772a)
- Fix README image path to correctly display app icon (#1943679)

### 📝 Documentation
- Center and resize app icon in README for better appearance (#cf3a1bc)
- Add direct download link for latest release

## [1.0.2] - 2026-02-01

- Initial public release
- Global voice typing with AI refinement
- Support for Groq and OpenAI providers
- Customizable refinement presets
- System tray integration
