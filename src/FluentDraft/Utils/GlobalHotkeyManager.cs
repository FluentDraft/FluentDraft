using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FluentDraft.Utils
{
    public class GlobalHotkeyManager : IDisposable
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private List<int> _monitoredKeys = new List<int> { 0x14 };
        private bool _isPressed = false;

        /// <summary>
        /// If true, the monitored key event will be swallowed and not passed to other applications.
        /// </summary>
        public bool IsSuppressionEnabled { get; set; } = false;

        public event Action? HotkeyDown;
        public event Action? Hotkeyup;

        public GlobalHotkeyManager()
        {
            _proc = HookCallback;
            try
            {
                _hookID = SetHook(_proc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set hook: {ex}");
            }
        }

        public void SetMonitoredKeys(IEnumerable<int> vKeys)
        {
            _monitoredKeys = vKeys.ToList();
            if (_monitoredKeys.Count == 0) _monitoredKeys.Add(0x14); // Fallback
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _monitoredKeys.Count > 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (_monitoredKeys.Contains(vkCode))
                {
                    bool isKeyDown = (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN);
                    bool isKeyUp = (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP);

                    if (isKeyDown)
                    {
                        if (!_isPressed)
                        {
                            _isPressed = true;
                            HotkeyDown?.Invoke();
                        }
                    }
                    else if (isKeyUp)
                    {
                        if (_isPressed)
                        {
                            _isPressed = false;
                            Hotkeyup?.Invoke();
                        }
                    }

                    if (IsSuppressionEnabled)
                    {
                        return (IntPtr)1; // Block the key
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}
