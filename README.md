# FluentDraft 🎙️

**Global Voice Typing for Windows** — Powered by Groq, OpenAI, and Whisper.

FluentDraft is a high-performance Windows desktop application that brings AI-powered voice typing to any application. Hold a hotkey, speak, and watch your refined, perfectly puntucated text appear instantly.

![FluentDraft App Icon](Icons/app_icon.png)

## ✨ Features

- **Global Hotkey** — Dictate into any active window (Notepad, Browser, Slack, IDE).
- **AI Refinement** — Automatically corrects grammar, punctuation, and style using LLMs (Llama 3, GPT-4).
- **Refinement Presets** — Create custom presets for different contexts (e.g., "Casual Chat", "Professional Email", "Code Comment").
- **High Fidelity Recording** — Uses `NAudio` for crystal clear voice capture.
- **Visual Feedback** — Minimalist, dark-themed UI with real-time waveform animation.
- **Provider Agnostic** — Supports Groq (fastest) and OpenAI, with extensible architecture.

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET 8 Runtime

### Installation
1. Clone the repository
   ```bash
   git clone https://github.com/FluentDraft/FluentDraft.git
   ```
2. Build the project
   ```bash
   dotnet build
   ```
3. Run the application
   ```bash
   dotnet run --project FluentDraft
   ```

## ⚙️ Configuration

1. **API Keys**: Launch the app and go to Settings. Enter your API Key for Groq or OpenAI.
2. **Hotkeys**: Default hotkey is `CapsLock` (configurable in settings).
3. **Microphone**: Select your preferred input device.

## 🛠️ Built With

- **C# / .NET 8**
- **WPF** (Windows Presentation Foundation)
- **CommunityToolkit.Mvvm**
- **NAudio**
- **H.NotifyIcon**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
