using System.Collections.Generic;
using NAudio.Wave;

namespace FluentDraft.Services
{
    public class AudioDeviceService
    {
        public List<(int DeviceNumber, string ProductName)> GetRecordingDevices()
        {
            var devices = new List<(int, string)>();
            for (int n = -1; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                devices.Add((n, caps.ProductName));
            }
            return devices;
        }
    }
}
