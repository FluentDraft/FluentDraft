using System;
using System.Threading.Tasks;
using FluentDraft.Services.Interfaces;
using Velopack;
using Velopack.Sources;

namespace FluentDraft.Services.Implementations
{
    /// <summary>
    /// Service for checking and applying automatic updates from GitHub Releases.
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private const string GITHUB_REPO_URL = "https://github.com/FluentDraft/FluentDraft";
        private readonly ILoggingService _logger;
        private UpdateManager? _updateManager;

        public UpdateService(ILoggingService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Checks if an update is available and returns the new version info.
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                EnsureUpdateManager();
                var updateInfo = await _updateManager!.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    _logger.LogInfo($"Update available: {updateInfo.TargetFullRelease.Version}");
                }
                
                return updateInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking for updates", ex);
                return null;
            }
        }

        /// <summary>
        /// Downloads the update package.
        /// </summary>
        public async Task DownloadUpdateAsync(UpdateInfo updateInfo, Action<int>? progress = null)
        {
            try
            {
                EnsureUpdateManager();
                _logger.LogInfo($"Downloading update: {updateInfo.TargetFullRelease.Version}");
                await _updateManager!.DownloadUpdatesAsync(updateInfo, progress);
                _logger.LogInfo("Update downloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error downloading update", ex);
                throw;
            }
        }

        /// <summary>
        /// Applies the downloaded update and restarts the application.
        /// </summary>
        public void ApplyUpdateAndRestart(UpdateInfo updateInfo)
        {
            try
            {
                EnsureUpdateManager();
                _logger.LogInfo("Applying update and restarting...");
                _updateManager!.ApplyUpdatesAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying update", ex);
                throw;
            }
        }

        /// <summary>
        /// Convenience method: checks, downloads, and applies updates in one call.
        /// Returns true if an update was found and applied (app will restart).
        /// </summary>
        public async Task<bool> CheckDownloadAndApplyAsync(Action<int>? downloadProgress = null)
        {
            var updateInfo = await CheckForUpdatesAsync();
            
            if (updateInfo == null)
            {
                _logger.LogInfo("No updates available");
                return false;
            }

            await DownloadUpdateAsync(updateInfo, downloadProgress);
            ApplyUpdateAndRestart(updateInfo);
            
            return true; // Note: This line may not execute as the app restarts
        }

        /// <summary>
        /// Gets the current installed version.
        /// </summary>
        public string? GetCurrentVersion()
        {
            try
            {
                EnsureUpdateManager();
                return _updateManager?.CurrentVersion?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if running from an installed Velopack package (not debug/portable).
        /// </summary>
        public bool IsInstalledApp()
        {
            try
            {
                EnsureUpdateManager();
                return _updateManager?.IsInstalled ?? false;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureUpdateManager()
        {
            _updateManager ??= new UpdateManager(new GithubSource(GITHUB_REPO_URL, null, false));
        }
    }
}
