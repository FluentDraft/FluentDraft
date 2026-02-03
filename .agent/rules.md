# FluentDraft Agent Rules

Follow these rules when modifying the codebase.

## 🎨 UI & Aesthetics
- **Wow Factor**: Use rich aesthetics, gradients, and modern typography (Inter/Outfit).
- **Dark Mode**: Maintain the dark, premium theme (#181818 background).
- **Responsiveness**: Ensure windows and controls handle different scales and localizations.
- **No Placeholders**: Generate real assets using tools if needed.

## 💻 Code Quality
- **MVVM**: Strictly follow the MVVM pattern using `CommunityToolkit.Mvvm`. Use `[ObservableProperty]` and `[RelayCommand]`.
- **DI**: Register all services in `App.xaml.cs`.
- **Clean Code**: Avoid redundant `Debug.WriteLine` in production. Use `ILoggingService`.

## 🌍 Localization
- **Always Multi-Language**: If you add a string, add it to all `Strings.*.xaml` files. 
- **Use DynamicResource**: Always bind UI text using `{DynamicResource S_KeyName}`.

## 🔄 Versioning & Release
- **Conventional Commits**: Use `feat:`, `fix:`, `chore:`, `docs:`.
- **Changelog**: Always update `CHANGELOG.md` when bumping version.
- **Version Check**: Versioning is managed in `src/FluentDraft/FluentDraft.csproj`.
