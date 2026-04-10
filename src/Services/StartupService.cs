using System;
using Microsoft.Win32;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Manages the "launch on Windows startup" Registry entry.
    /// Writes to HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run — no admin rights required.
    /// </summary>
    public class StartupService
    {
        private const string RegistryKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName      = "DockyJumpList";

        /// <summary>Returns true if Docky is registered to run at Windows startup.</summary>
        public bool IsEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: false);
                    return key?.GetValue(AppName) != null;
                }
                catch { return false; }
            }
        }

        /// <summary>Registers or removes the startup entry.</summary>
        public void SetEnabled(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
                if (key == null) return;

                if (enable)
                    key.SetValue(AppName, $"\"{ExecutablePath}\"");
                else
                    key.DeleteValue(AppName, throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not update startup setting:\n{ex.Message}",
                    "Docky Jump List",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        private static string ExecutablePath =>
            System.Reflection.Assembly.GetExecutingAssembly().Location;
    }
}
