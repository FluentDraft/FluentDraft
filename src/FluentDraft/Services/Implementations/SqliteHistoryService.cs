using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentDraft.Data;
using FluentDraft.Models;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class SqliteHistoryService : IHistoryService
    {
        private readonly IDbContextFactory<FluentDraftDbContext> _contextFactory;
        private readonly ILoggingService _logger;
        private bool _isMigrated = false;

        public SqliteHistoryService(
            IDbContextFactory<FluentDraftDbContext> contextFactory,
            ILoggingService logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _ = InitializeSchema();
        }

        private async Task InitializeSchema()
        {
            try
            {
               using var context = await _contextFactory.CreateDbContextAsync();
               // Ensure database exists first
               await context.Database.EnsureCreatedAsync();

               // Patch for new columns if they don't exist
               var columnsToAdd = new[] 
               {
                   ("RawTranscription", "TEXT"),
                   ("TranscriptionModel", "TEXT"),
                   ("RefinementPresetId", "TEXT"),
                   ("RefinementPresetName", "TEXT")
               };

               foreach (var (col, type) in columnsToAdd)
               {
                   try 
                   {
                       // This will fail if column exists, which is fine
                       await context.Database.ExecuteSqlRawAsync($"ALTER TABLE AudioRecordings ADD COLUMN {col} {type}");
                       _logger.LogInfo($"Added column {col} to AudioRecordings table.");
                   }
                   catch { /* Ignore if column exists */ }
               }
            }
            catch (Exception ex)
            {
               _logger.LogError("Error initializing schema", ex);
            }
        }

        public async Task<List<TranscriptionItem>> GetHistoryAsync()
        {
            await EnsureMigratedAsync();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var entities = await context.AudioRecordings
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return entities.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading history from SQLite", ex);
                return new List<TranscriptionItem>();
            }
        }

        public async Task AddAsync(TranscriptionItem item)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var entity = MapToEntity(item);
                context.AudioRecordings.Add(entity);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding history item", ex);
            }
        }

        public async Task UpdateAsync(TranscriptionItem item)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var entity = await context.AudioRecordings.FirstOrDefaultAsync(r => r.Id == item.Id);
                if (entity != null)
                {
                    entity.TranscriptionText = item.Text;
                    entity.RawTranscription = item.RawText;
                    entity.TranscriptionModel = item.TranscriptionModel;
                    entity.RefinementPresetId = item.RefinementPresetId;
                    entity.RefinementPresetName = item.RefinementPresetName;
                    
                    context.AudioRecordings.Update(entity);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating history item", ex);
            }
        }

        public async Task DeleteAsync(TranscriptionItem item)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var entity = await context.AudioRecordings.FirstOrDefaultAsync(r => r.Id == item.Id);
                if (entity != null)
                {
                    context.AudioRecordings.Remove(entity);
                    await context.SaveChangesAsync();

                    // Optionally delete file? keeping consistent with previous behavior (cache remove only, no file delete?)
                    // Previous JSONHistoryService commented "Optionally delete audio file?".
                    // We will stick to just DB removal for now to avoid data loss accidents.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting history item", ex);
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                // Batch delete not supported in older EF Core efficiently without raw SQL or loading all.
                // SQLite supports ExecuteDeleteAsync in EF Core 7+. Assuming we are on new enough version.
                // If not, we remove range.
                context.AudioRecordings.RemoveRange(context.AudioRecordings);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error clearing history", ex);
            }
        }

        private async Task EnsureMigratedAsync()
        {
            if (_isMigrated) return;

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                if (!await context.AudioRecordings.AnyAsync())
                {
                    // Check for history.json
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var jsonPath = Path.Combine(appData, "FluentDraft", "history.json");

                    if (File.Exists(jsonPath))
                    {
                        _logger.LogInfo("Migrating history from JSON to SQLite...");
                        try
                        {
                            var json = await File.ReadAllTextAsync(jsonPath);
                            var oldItems = JsonSerializer.Deserialize<List<TranscriptionItem>>(json);
                            if (oldItems != null && oldItems.Any())
                            {
                                var newAudioDir = Path.Combine(appData, "FluentDraft", "Audio");
                                if (!Directory.Exists(newAudioDir)) Directory.CreateDirectory(newAudioDir);

                                var entities = new List<AudioRecordingEntity>();
                                foreach (var item in oldItems)
                                {
                                    var entity = MapToEntity(item);

                                    // Try to move file if it exists
                                    if (!string.IsNullOrEmpty(entity.FilePath) && File.Exists(entity.FilePath))
                                    {
                                        try
                                        {
                                            var fileName = Path.GetFileName(entity.FilePath);
                                            var newPath = Path.Combine(newAudioDir, fileName);

                                            if (!File.Exists(newPath))
                                            {
                                                File.Move(entity.FilePath, newPath);
                                            }
                                            entity.FilePath = newPath;
                                        }
                                        catch (Exception moveEx)
                                        {
                                            _logger.LogError($"Failed to move audio file for item {item.Id}", moveEx);
                                            // Keep old path if move failed
                                        }
                                    }
                                    entities.Add(entity);
                                }

                                context.AudioRecordings.AddRange(entities);
                                await context.SaveChangesAsync();

                                // Rename old file
                                File.Move(jsonPath, jsonPath + ".bak");
                                _logger.LogInfo($"Migrated {entities.Count} history items.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Migration failed for history.json", ex);
                        }
                    }
                }
                _isMigrated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking migration status", ex);
            }
        }

        private TranscriptionItem MapToModel(AudioRecordingEntity entity)
        {
            return new TranscriptionItem
            {
                Id = entity.Id,
                Text = entity.TranscriptionText,
                RawText = entity.RawTranscription,
                TranscriptionModel = entity.TranscriptionModel,
                RefinementPresetId = entity.RefinementPresetId,
                RefinementPresetName = entity.RefinementPresetName,
                AudioFilePath = entity.FilePath,
                Timestamp = entity.CreatedAt
            };
        }

        private AudioRecordingEntity MapToEntity(TranscriptionItem model)
        {
            return new AudioRecordingEntity
            {
                Id = model.Id,
                TranscriptionText = model.Text,
                RawTranscription = model.RawText,
                TranscriptionModel = model.TranscriptionModel,
                RefinementPresetId = model.RefinementPresetId,
                RefinementPresetName = model.RefinementPresetName,
                FilePath = model.AudioFilePath ?? "",
                CreatedAt = model.Timestamp,
                Duration = TimeSpan.Zero, // Not captured in old model, default zero
                IsProcessed = true
            };
        }
    }
}
