using System;
using System.Threading.Tasks;
using WindowsInput;

using FluentDraft.Services.Interfaces;
using Windows.Media.Control;
using System.Runtime.InteropServices;

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

        public IntPtr GetForegroundWindowHandle()
        {
            return GetForegroundWindow();
        }

        public bool SetForegroundWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return false;
            // Often needed if we are not the active window trying to set another
            // But if we are the active window (user clicked us), we should be able to set it.
            // Sometimes AttachThreadInput is needed, but let's try simple way first.
            return SetForegroundWindowImport(handle);
        }

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindowImport(IntPtr hWnd);
    }
}
