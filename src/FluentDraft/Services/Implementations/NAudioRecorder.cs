using System;
using System.IO;
using NAudio.Wave;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class NAudioRecorder : IAudioRecorder, IDisposable
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private string? _audioFilePath;
        private bool _isRecording;
        private readonly ILoggingService _logger;
        private readonly string _audioDir;
        private TaskCompletionSource? _stopTcs;
        private double _volumeLevel;

        public bool IsRecording => _isRecording;
        public double VolumeLevel => _volumeLevel;
        public int DeviceNumber { get; set; } = 0;
        public event Action<double>? VolumeChanged;

        public NAudioRecorder(ILoggingService logger)
        {
            _logger = logger;
            _audioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio");
            if (!Directory.Exists(_audioDir))
            {
                Directory.CreateDirectory(_audioDir);
            }
        }

        public void StartRecording()
        {
            if (_isRecording) return;

            try
            {
                _isRecording = true;
                _stopTcs = new TaskCompletionSource();
                _audioFilePath = Path.Combine(_audioDir, $"rec_{DateTime.Now:yyyyMMddHHmmss}.wav");
                _waveIn = new WaveInEvent();
                _waveIn.DeviceNumber = DeviceNumber;
                _waveIn.WaveFormat = new WaveFormat(16000, 1);
                _waveIn.BufferMilliseconds = 50; // Lower latency for smoother UI
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += WaveIn_RecordingStopped;

                _writer = new WaveFileWriter(_audioFilePath, _waveIn.WaveFormat);
                _waveIn.StartRecording();
                _logger.LogInfo($"Recording started on device {DeviceNumber}: {_audioFilePath}");
            }
            catch (Exception ex)
            {
                _isRecording = false;
                _logger.LogError("Failed to start recording", ex);
                Cleanup();
                throw;
            }
        }

        public async Task StopRecordingAsync()
        {
            if (!_isRecording) return;

            try
            {
                _waveIn?.StopRecording();
                _isRecording = false;
                _logger.LogInfo("Recording stop requested.");
                
                if (_stopTcs != null)
                {
                    await _stopTcs.Task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error stopping recording", ex);
            }
        }

        public string GetRecordedFilePath()
        {
            return _audioFilePath ?? string.Empty;
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                _writer?.Write(e.Buffer, 0, e.BytesRecorded);

                // Calculate peak volume for visualizer
                float max = 0;
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                    float sample32 = sample / 32768f;
                    if (sample32 < 0) sample32 = -sample32;
                    if (sample32 > max) max = sample32;
                }
                
                _volumeLevel = max;
                VolumeChanged?.Invoke(max);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error writing audio data", ex);
            }
        }

        private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            Cleanup();
            _stopTcs?.TrySetResult();
            _logger.LogInfo("Recording fully stopped and file released.");
        }

        private void Cleanup()
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
        }

        public void Dispose()
        {
            if (_isRecording)
            {
                _waveIn?.StopRecording();
            }
            Cleanup();
        }
    }
}
