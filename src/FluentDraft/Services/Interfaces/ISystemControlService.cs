using System.Threading.Tasks;

namespace FluentDraft.Services.Interfaces
{
    public interface ISystemControlService
    {
        Task<bool> PauseMediaIfPlayingAsync();
        Task ResumeMediaAsync();
        Task ToggleMicrophoneMuteAsync(); // Retain just in case
        Task UnmuteSystemAudioAsync();

        System.IntPtr GetForegroundWindowHandle();
        bool SetForegroundWindow(System.IntPtr handle);
    }
}
