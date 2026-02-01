using System;
using System.Threading.Tasks;
using Velopack;

namespace FluentDraft.Services.Interfaces
{
    /// <summary>
    /// Interface for the update service that checks and applies updates from GitHub.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Checks if an update is available.
        /// </summary>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>
        /// Downloads the update package.
        /// </summary>
        Task DownloadUpdateAsync(UpdateInfo updateInfo, Action<int>? progress = null);

        /// <summary>
        /// Applies the downloaded update and restarts the application.
        /// </summary>
        void ApplyUpdateAndRestart(UpdateInfo updateInfo);

        /// <summary>
        /// Convenience method: checks, downloads, and applies updates in one call.
        /// </summary>
        Task<bool> CheckDownloadAndApplyAsync(Action<int>? downloadProgress = null);

        /// <summary>
        /// Gets the current installed version.
        /// </summary>
        string? GetCurrentVersion();

        /// <summary>
        /// Returns true if running from an installed Velopack package.
        /// </summary>
        bool IsInstalledApp();
    }
}
