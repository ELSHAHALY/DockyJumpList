using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Registers a global hotkey using Win32 RegisterHotKey.
    /// Default: Ctrl+Alt+D — opens the jump list from anywhere on the system.
    ///
    /// Usage:
    ///   var hotkey = new GlobalHotkeyService();
    ///   hotkey.HotkeyPressed += () => ShowJumpList();
    ///   hotkey.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.D);
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        // Win32 constants
        private const int WM_HOTKEY  = 0x0312;
        private const int HOTKEY_ID  = 0xBEEF;   // Arbitrary unique ID for this app

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifier flags (matching System.Windows.Input.ModifierKeys values)
        public const uint MOD_ALT     = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT   = 0x0004;
        public const uint MOD_WIN     = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        private HwndSource _source;
        private bool _registered;

        public event Action HotkeyPressed;

        /// <summary>
        /// Registers the hotkey. Call once after the application is initialized.
        /// modifiers: combination of MOD_* constants.
        /// virtualKey: Win32 virtual key code (e.g. (uint)'D' for the D key).
        /// </summary>
        public bool Register(uint modifiers, uint virtualKey)
        {
            // We need a hidden Win32 window handle to receive WM_HOTKEY messages.
            // Use HwndSource to create a message-only window.
            var parameters = new HwndSourceParameters("DockyHotkeyWindow")
            {
                WindowStyle  = 0,
                ExtendedWindowStyle = 0,
                PositionX    = 0,
                PositionY    = 0,
                Width        = 0,
                Height       = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE — message-only window
            };

            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);

            _registered = RegisterHotKey(
                _source.Handle,
                HOTKEY_ID,
                modifiers | MOD_NOREPEAT,
                virtualKey);

            return _registered;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_registered && _source != null)
                UnregisterHotKey(_source.Handle, HOTKEY_ID);

            _source?.Dispose();
        }
    }
}
