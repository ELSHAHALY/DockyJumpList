using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using DockyJumpList.Models;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Manages the in-memory list of shortcuts and persists them to a JSON file
    /// in %AppData%\DockyJumpList\shortcuts.json.
    /// </summary>
    public class ShortcutService
    {
        private static readonly string DataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DockyJumpList");

        private static readonly string DataFile =
            Path.Combine(DataFolder, "shortcuts.json");

        private List<ShortcutItem> _shortcuts = new();

        /// <summary>Raised whenever the list is modified, so the tray menu can rebuild.</summary>
        public event Action ShortcutsChanged;

        // ── Read ──────────────────────────────────────────────────────────────

        public List<ShortcutItem> GetAll() =>
            _shortcuts.OrderBy(s => s.SortOrder).ThenBy(s => s.DisplayName).ToList();

        // ── CRUD ──────────────────────────────────────────────────────────────

        public void Add(ShortcutItem item)
        {
            item.SortOrder = _shortcuts.Count;
            _shortcuts.Add(item);
            Save();
            ShortcutsChanged?.Invoke();
        }

        public void Update(ShortcutItem updated)
        {
            var existing = _shortcuts.FirstOrDefault(s => s.Id == updated.Id);
            if (existing == null) return;

            existing.DisplayName = updated.DisplayName;
            existing.Target      = updated.Target;
            existing.Arguments   = updated.Arguments;
            existing.SortOrder   = updated.SortOrder;

            Save();
            ShortcutsChanged?.Invoke();
        }

        public void Remove(Guid id)
        {
            _shortcuts.RemoveAll(s => s.Id == id);
            Save();
            ShortcutsChanged?.Invoke();
        }

        public void Reorder(List<ShortcutItem> ordered)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                var item = _shortcuts.FirstOrDefault(s => s.Id == ordered[i].Id);
                if (item != null) item.SortOrder = i;
            }
            Save();
            ShortcutsChanged?.Invoke();
        }

        // ── Launch ────────────────────────────────────────────────────────────

        /// <summary>
        /// Launches an application or opens a URL using the system default handler.
        /// </summary>
        public void Launch(ShortcutItem item)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true
                };

                if (item.Type == ShortcutType.Url)
                {
                    psi.FileName = item.Target;
                }
                else
                {
                    psi.FileName  = item.Target;
                    psi.Arguments = item.Arguments ?? string.Empty;
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not launch '{item.DisplayName}':\n{ex.Message}",
                    "Docky Jump List",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────

        public void Load()
        {
            try
            {
                if (!File.Exists(DataFile))
                {
                    _shortcuts = new List<ShortcutItem>();
                    return;
                }

                var json = File.ReadAllText(DataFile);
                _shortcuts = JsonSerializer.Deserialize<List<ShortcutItem>>(json)
                             ?? new List<ShortcutItem>();
            }
            catch
            {
                _shortcuts = new List<ShortcutItem>();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                var json = JsonSerializer.Serialize(_shortcuts, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to save shortcuts:\n{ex.Message}",
                    "Docky Jump List",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
