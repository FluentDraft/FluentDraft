using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace FluentDraft.Utils
{
    public class GlobalHotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly DispatcherTimer _timer;
        private bool _isAllPressed;
        private List<int> _monitoredKeys = new List<int> { 0x14 }; // Default to CapsLock

        public event Action? HotkeyDown;
        public event Action? Hotkeyup;

        public GlobalHotkeyManager()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        public void SetMonitoredKeys(IEnumerable<int> vKeys)
        {
            _monitoredKeys = vKeys.ToList();
            if (_monitoredKeys.Count == 0) _monitoredKeys.Add(0x14); // Fallback
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_monitoredKeys.Count == 0) return;

            bool allPressed = true;
            foreach (var key in _monitoredKeys)
            {
                short state = GetAsyncKeyState(key);
                if ((state & 0x8000) == 0)
                {
                    allPressed = false;
                    break;
                }
            }

            if (allPressed && !_isAllPressed)
            {
                _isAllPressed = true;
                HotkeyDown?.Invoke();
            }
            else if (!allPressed && _isAllPressed)
            {
                _isAllPressed = false;
                Hotkeyup?.Invoke();
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}
