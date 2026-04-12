using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Shell;
using DockyJumpList.Models;

namespace DockyJumpList.src.Services {
    /// <summary>
    /// Builds and refreshes the Windows taskbar Jump List from the user's shortcuts.
    ///
    /// The Jump List is the native context menu that Windows shows when the user
    /// right-clicks (or long-presses) the app's taskbar button.  It is separate from
    /// the in-app tray <see cref="System.Windows.Forms.ContextMenuStrip"/>.
    ///
    /// How it works:
    ///   - Each <see cref="ShortcutItem"/> becomes a <see cref="JumpTask"/> (exe) or
    ///     a <see cref="JumpPath"/> (URL via a .url shell link).
    ///   - <see cref="JumpTask"/> uses <c>ApplicationPath</c> + optional
    ///     <c>Arguments</c>, so Windows can launch the target directly.
    ///   - URLs cannot be <see cref="JumpTask"/> targets directly; we write a
    ///     temporary .url file and add it as a <see cref="JumpPath"/> instead.
    ///   - <see cref="Refresh"/> is idempotent and safe to call whenever shortcuts
    ///     change (the <see cref="JumpList"/> replaces itself on each call).
    ///
    /// Limitations:
    ///   - Jump Lists only work when the app has an associated taskbar button, i.e.
    ///     when it is pinned to the taskbar or currently running.
    ///   - URL .url helper files are written to %AppData%\DockyJumpList\jumplist-urls\
    ///     and are reused across calls (one file per shortcut ID).
    /// </summary>
    public class JumpListService {
        // ── Paths ─────────────────────────────────────────────────────────────

        private static readonly string UrlCacheFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DockyJumpList",
                "jumplist-urls");

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the Windows Jump List to match <paramref name="shortcuts"/>.
        /// Safe to call on any thread — marshals to the UI thread internally via
        /// <see cref="System.Windows.Application.Current"/>.
        /// </summary>
        public void Refresh(IEnumerable<ShortcutItem> shortcuts) {
            // JumpList must be mutated on the UI (Dispatcher) thread.
            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.Invoke(() => RebuildOnUiThread(shortcuts));
        }

        // ── Implementation ────────────────────────────────────────────────────

        private void RebuildOnUiThread(IEnumerable<ShortcutItem> shortcuts) {
            var jumpList = new JumpList();
            jumpList.ShowRecentCategory = false;
            jumpList.ShowFrequentCategory = false;

            EnsureUrlCacheFolder();

            foreach (var sc in shortcuts) {
                try {
                    JumpItem item = sc.Type == ShortcutType.Url
                        ? BuildUrlJumpPath(sc)
                        : BuildAppJumpTask(sc);

                    if (item != null)
                        jumpList.JumpItems.Add(item);
                }
                catch {
                    // Skip malformed entries — never crash the app.
                }
            }

            JumpList.SetJumpList(System.Windows.Application.Current, jumpList);
            jumpList.Apply();
        }

        /// <summary>
        /// Creates a <see cref="JumpTask"/> that launches an .exe with optional args.
        /// </summary>
        private static JumpTask BuildAppJumpTask(ShortcutItem sc) {
            if (!File.Exists(sc.Target))
                return null; // Don't add broken shortcuts.

            return new JumpTask {
                Title = sc.DisplayName,
                ApplicationPath = sc.Target,
                Arguments = sc.Arguments ?? string.Empty,
                IconResourcePath = sc.Target,   // use the exe's own icon
                IconResourceIndex = 0,
                CustomCategory = "Shortcuts"
            };
        }

        /// <summary>
        /// Creates a <see cref="JumpPath"/> pointing to a .url shell-link file.
        /// Windows Explorer knows how to open .url files with the default browser.
        /// </summary>
        private JumpPath BuildUrlJumpPath(ShortcutItem sc) {
            var urlFilePath = WriteUrlFile(sc);
            if (urlFilePath == null) return null;

            return new JumpPath {
                Path = urlFilePath,
                CustomCategory = "Shortcuts"
            };
        }

        /// <summary>
        /// Writes (or overwrites) a Windows Internet Shortcut (.url) file for the
        /// given shortcut and returns its full path, or <c>null</c> on failure.
        /// </summary>
        private static string WriteUrlFile(ShortcutItem sc) {
            try {
                // Use a stable filename based on the shortcut's GUID so the same
                // file is reused when the shortcut is edited.
                var fileName = $"{sc.Id:N}.url";
                var filePath = Path.Combine(UrlCacheFolder, fileName);

                // Windows Internet Shortcut format — understood by Explorer / shell.
                var contents = $"[InternetShortcut]\r\nURL={sc.Target}\r\n";
                File.WriteAllText(filePath, contents);

                return filePath;
            }
            catch {
                return null;
            }
        }

        private static void EnsureUrlCacheFolder() {
            try { Directory.CreateDirectory(UrlCacheFolder); }
            catch { /* ignore */ }
        }
    }
}