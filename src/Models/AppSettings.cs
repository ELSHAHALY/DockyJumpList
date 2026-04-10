namespace DockyJumpList.Models
{
    /// <summary>
    /// User preferences persisted alongside shortcuts.
    /// Stored at %AppData%\DockyJumpList\settings.json.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Launch Docky automatically when Windows starts.</summary>
        public bool LaunchOnStartup { get; set; } = false;

        /// <summary>
        /// Global hotkey modifier flags (sum of GlobalHotkeyService.MOD_* constants).
        /// Default: Ctrl+Alt = MOD_CONTROL | MOD_ALT = 0x0002 | 0x0001 = 3
        /// </summary>
        public uint HotkeyModifiers { get; set; } = 0x0003;

        /// <summary>
        /// Virtual key code for the global hotkey trigger.
        /// Default: 0x44 = VK_D (the D key).
        /// </summary>
        public uint HotkeyVirtualKey { get; set; } = 0x44;

        /// <summary>Human-readable label shown in the settings panel.</summary>
        public string HotkeyDisplayLabel { get; set; } = "Ctrl + Alt + D";

        /// <summary>Show application icons next to each shortcut name in the jump list.</summary>
        public bool ShowIconsInMenu { get; set; } = true;
    }
}
