using System;
using System.IO;
using System.Threading.Tasks;

namespace FluentDraft.Services.Interfaces
{
    public interface IAudioRecorder
    {
        void StartRecording();
        Task StopRecordingAsync();
        string GetRecordedFilePath();
        bool IsRecording { get; }
        double VolumeLevel { get; }
        event Action<double> VolumeChanged;
    }
}
