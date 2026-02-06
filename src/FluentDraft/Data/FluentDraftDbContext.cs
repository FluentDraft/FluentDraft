using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FluentDraft.Data
{
    public class FluentDraftDbContext : DbContext
    {
        public DbSet<AppSettingEntity> AppSettings { get; set; }
        public DbSet<ProviderProfileEntity> ProviderProfiles { get; set; }
        public DbSet<RefinementPresetEntity> RefinementPresets { get; set; }
        public DbSet<AudioRecordingEntity> AudioRecordings { get; set; }

        public string DbPath { get; }

        public FluentDraftDbContext()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(folder, "FluentDraft");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            DbPath = Path.Combine(path, "fluentdraft.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
