using System;
using System.IO;
using System.Text.Json;
using DockyJumpList.Models;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Persists AppSettings to %AppData%\DockyJumpList\settings.json.
    /// </summary>
    public class SettingsService
    {
        private static readonly string DataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DockyJumpList");

        private static readonly string SettingsFile =
            Path.Combine(DataFolder, "settings.json");

        private AppSettings _current;
        public AppSettings Current => _current ??= Load();

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    _current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _current = new AppSettings();
                }
            }
            catch
            {
                _current = new AppSettings();
            }

            return _current;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to save settings:\n{ex.Message}",
                    "Docky Jump List",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
