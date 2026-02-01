using System;
using System.Threading.Tasks;
using WindowsInput;

using FluentDraft.Services.Interfaces;
using Windows.Media.Control;

namespace FluentDraft.Services.Implementations
{
    public class WindowsSystemControlService : ISystemControlService
    {
        private readonly IInputSimulator _inputSimulator;

        public WindowsSystemControlService()
        {
            _inputSimulator = new InputSimulator();
        }

        public async Task<bool> PauseMediaIfPlayingAsync()
        {
            try
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var session = manager.GetCurrentSession();

                if (session != null)
                {
                    var info = session.GetPlaybackInfo();
                    if (info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        await session.TryPauseAsync();
                        return true; // We successfully paused it
                    }
                }
            }
            catch
            {
                // Fallback or ignore if API fails
            }
            return false;
        }

        public async Task ResumeMediaAsync()
        {
            try
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var session = manager.GetCurrentSession();

                if (session != null)
                {
                    var info = session.GetPlaybackInfo();
                    if (info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused)
                    {
                        await session.TryPlayAsync();
                    }
                }
            }
            catch
            {
                // Fallback
            }
        }

        public Task ToggleMicrophoneMuteAsync()
        {
            // Implementation for Microphone Mute if needed via key press
            // or specific API. For now, we can keep the media key pattern or empty if not used.
            return Task.CompletedTask;
        }

        public Task UnmuteSystemAudioAsync()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VOLUME_MUTE);
            return Task.CompletedTask;
        }
    }
}
