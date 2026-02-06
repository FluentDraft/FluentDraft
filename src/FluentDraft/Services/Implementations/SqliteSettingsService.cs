using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentDraft.Data;
using FluentDraft.Models;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class SqliteSettingsService : ISettingsService
    {
        private readonly IDbContextFactory<FluentDraftDbContext> _contextFactory;
        private readonly ISecureStorageService _secureStorage;
        private readonly ILoggingService _logger;

        public SqliteSettingsService(
            IDbContextFactory<FluentDraftDbContext> contextFactory,
            ISecureStorageService secureStorage,
            ILoggingService logger)
        {
            _contextFactory = contextFactory;
            _secureStorage = secureStorage;
            _logger = logger;
            
            // Ensure DB is created
            using var context = _contextFactory.CreateDbContext();
            context.Database.EnsureCreated();
        }
        
        private bool _isMigrated = false;

        public async Task<AppSettings> LoadSettingsAsync()
        {
            await EnsureMigratedAsync();

            var settings = new AppSettings();
            
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Load Key-Value Settings
                var kvps = await context.AppSettings.ToListAsync();
                foreach (var kvp in kvps)
                {
                    ApplySetting(settings, kvp.Key, kvp.Value);
                }

                // Load Providers
                var providerEntities = await context.ProviderProfiles.ToListAsync();
                settings.Providers = providerEntities.Select(MapToModel).ToList();

                // Load Presets
                var presetEntities = await context.RefinementPresets.ToListAsync();
                settings.RefinementPresets = presetEntities.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading settings from SQLite", ex);
            }

            return settings;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Save Key-Value Settings
                UpdateSetting(context, nameof(AppSettings.LanguageCode), settings.LanguageCode);
                UpdateSetting(context, nameof(AppSettings.IsAlwaysOnTop), settings.IsAlwaysOnTop.ToString());
                UpdateSetting(context, nameof(AppSettings.IsHotkeySuppressionEnabled), settings.IsHotkeySuppressionEnabled.ToString());
                UpdateSetting(context, nameof(AppSettings.ActivationMode), settings.ActivationMode.ToString());
                UpdateSetting(context, nameof(AppSettings.PlaySoundOnRecord), settings.PlaySoundOnRecord.ToString());
                UpdateSetting(context, nameof(AppSettings.SelectedMicrophone), settings.SelectedMicrophone.ToString());
                UpdateSetting(context, nameof(AppSettings.TextInjectionMode), settings.TextInjectionMode.ToString());
                UpdateSetting(context, nameof(AppSettings.IsAutoInsertEnabled), settings.IsAutoInsertEnabled.ToString());
                UpdateSetting(context, nameof(AppSettings.CloseToTray), settings.CloseToTray.ToString());
                UpdateSetting(context, nameof(AppSettings.MaxRecordingSeconds), settings.MaxRecordingSeconds.ToString());
                UpdateSetting(context, nameof(AppSettings.ChatSessionId), settings.ChatSessionId ?? "");
                UpdateSetting(context, nameof(AppSettings.IsDebugModeEnabled), settings.IsDebugModeEnabled.ToString());
                UpdateSetting(context, nameof(AppSettings.IsSetupCompleted), settings.IsSetupCompleted.ToString());
                UpdateSetting(context, nameof(AppSettings.IsPostProcessingEnabled), settings.IsPostProcessingEnabled.ToString());
                UpdateSetting(context, nameof(AppSettings.PostProcessingPrompt), settings.PostProcessingPrompt);
                UpdateSetting(context, nameof(AppSettings.PauseMediaOnRecording), settings.PauseMediaOnRecording.ToString());
                UpdateSetting(context, nameof(AppSettings.SelectedTranscriptionProfileId), settings.SelectedTranscriptionProfileId?.ToString() ?? "");
                UpdateSetting(context, nameof(AppSettings.SelectedRefinementProfileId), settings.SelectedRefinementProfileId?.ToString() ?? "");
                UpdateSetting(context, nameof(AppSettings.SelectedRefinementPresetId), settings.SelectedRefinementPresetId?.ToString() ?? "");
                UpdateSetting(context, nameof(AppSettings.HotkeyCodes), string.Join(",", settings.HotkeyCodes));

                // Save Providers
                // Simple strategy: Remove all and re-add? Or update/insert/delete?
                // For simplicity given the list size: Upsert. 
                // But deleting removed ones is tricky without tracking.
                // Let's use a full replacement strategy for simplicity or tracked entities if possible.
                // Since we are disconnected, full replacement of collection is easiest for small lists.
                
                context.ProviderProfiles.RemoveRange(context.ProviderProfiles);
                context.ProviderProfiles.AddRange(settings.Providers.Select(MapToEntity));

                // Save Presets
                context.RefinementPresets.RemoveRange(context.RefinementPresets);
                context.RefinementPresets.AddRange(settings.RefinementPresets.Select(MapToEntity));

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving settings to SQLite", ex);
            }
        }

        private void UpdateSetting(FluentDraftDbContext context, string key, string value)
        {
            var entity = context.AppSettings.FirstOrDefault(e => e.Key == key);
            if (entity == null)
            {
                entity = new AppSettingEntity { Key = key, Value = value };
                context.AppSettings.Add(entity);
            }
            else
            {
                entity.Value = value;
            }
        }

        private void ApplySetting(AppSettings settings, string key, string value)
        {
            switch (key)
            {
                case nameof(AppSettings.LanguageCode): settings.LanguageCode = value; break;
                case nameof(AppSettings.IsAlwaysOnTop): settings.IsAlwaysOnTop = bool.Parse(value); break;
                case nameof(AppSettings.IsHotkeySuppressionEnabled): settings.IsHotkeySuppressionEnabled = bool.Parse(value); break;
                case nameof(AppSettings.ActivationMode): settings.ActivationMode = int.Parse(value); break;
                case nameof(AppSettings.PlaySoundOnRecord): settings.PlaySoundOnRecord = bool.Parse(value); break;
                case nameof(AppSettings.SelectedMicrophone): settings.SelectedMicrophone = int.Parse(value); break;
                case nameof(AppSettings.TextInjectionMode): settings.TextInjectionMode = int.Parse(value); break;
                case nameof(AppSettings.IsAutoInsertEnabled): settings.IsAutoInsertEnabled = bool.Parse(value); break;
                case nameof(AppSettings.CloseToTray): settings.CloseToTray = bool.Parse(value); break;
                case nameof(AppSettings.MaxRecordingSeconds): settings.MaxRecordingSeconds = int.Parse(value); break;
                case nameof(AppSettings.ChatSessionId): settings.ChatSessionId = value; break;
                case nameof(AppSettings.IsDebugModeEnabled): settings.IsDebugModeEnabled = bool.Parse(value); break;
                case nameof(AppSettings.IsSetupCompleted): settings.IsSetupCompleted = bool.Parse(value); break;
                case nameof(AppSettings.IsPostProcessingEnabled): settings.IsPostProcessingEnabled = bool.Parse(value); break;
                case nameof(AppSettings.PostProcessingPrompt): settings.PostProcessingPrompt = value; break;
                case nameof(AppSettings.PauseMediaOnRecording): settings.PauseMediaOnRecording = bool.Parse(value); break;
                case nameof(AppSettings.SelectedTranscriptionProfileId): if (Guid.TryParse(value, out var g1)) settings.SelectedTranscriptionProfileId = g1; break;
                case nameof(AppSettings.SelectedRefinementProfileId): if (Guid.TryParse(value, out var g2)) settings.SelectedRefinementProfileId = g2; break;
                case nameof(AppSettings.SelectedRefinementPresetId): if (Guid.TryParse(value, out var g3)) settings.SelectedRefinementPresetId = g3; break;
                case nameof(AppSettings.HotkeyCodes): 
                    if (!string.IsNullOrEmpty(value)) 
                        settings.HotkeyCodes = value.Split(',').Select(int.Parse).ToList(); 
                    break;
            }
        }

        private async Task EnsureMigratedAsync()
        {
            if (_isMigrated) return;

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                if (!await context.AppSettings.AnyAsync() && !await context.ProviderProfiles.AnyAsync())
                {
                    // Check for settings.json
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var jsonPath = Path.Combine(appData, "FluentDraft", "settings.json");

                    if (System.IO.File.Exists(jsonPath))
                    {
                        _logger.LogInfo("Migrating settings from JSON to SQLite...");
                        try 
                        {
                            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
                            var oldSettings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                            if (oldSettings != null)
                            {
                                await SaveSettingsAsync(oldSettings);
                                
                                // Rename old file
                                System.IO.File.Move(jsonPath, jsonPath + ".bak");
                                _logger.LogInfo("Settings migrated successfully.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Migration failed for settings.json", ex);
                        }
                    }
                }
                _isMigrated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking settings migration status", ex);
            }
        }

        private ProviderProfile MapToModel(ProviderProfileEntity entity)
        {
            return new ProviderProfile
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Type,
                BaseUrl = entity.BaseUrl,
                ApiKey = _secureStorage.Decrypt(entity.EncryptedApiKey),
                TranscriptionModel = entity.TranscriptionModel,
                RefinementModel = entity.RefinementModel,
                IsTranscriptionEnabled = entity.IsTranscriptionEnabled,
                IsRefinementEnabled = entity.IsRefinementEnabled,
                IsValidated = true // Assume valid if stored? Or defaulting to false
            };
        }

        private ProviderProfileEntity MapToEntity(ProviderProfile model)
        {
            return new ProviderProfileEntity
            {
                Id = model.Id,
                Name = model.Name,
                Type = model.Type,
                BaseUrl = model.BaseUrl,
                EncryptedApiKey = _secureStorage.Encrypt(model.ApiKey),
                TranscriptionModel = model.TranscriptionModel,
                RefinementModel = model.RefinementModel,
                IsTranscriptionEnabled = model.IsTranscriptionEnabled,
                IsRefinementEnabled = model.IsRefinementEnabled
            };
        }

        private RefinementPreset MapToModel(RefinementPresetEntity entity)
        {
            return new RefinementPreset
            {
                Id = entity.Id,
                Name = entity.Name,
                ProfileId = entity.ProfileId,
                Model = entity.Model,
                SystemPrompt = entity.SystemPrompt
            };
        }

        private RefinementPresetEntity MapToEntity(RefinementPreset model)
        {
            return new RefinementPresetEntity
            {
                Id = model.Id,
                Name = model.Name,
                ProfileId = model.ProfileId,
                Model = model.Model,
                SystemPrompt = model.SystemPrompt
            };
        }
    }
}
