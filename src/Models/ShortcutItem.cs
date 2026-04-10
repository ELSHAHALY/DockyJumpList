using System;
using System.Drawing;
using System.IO;

namespace DockyJumpList.Models
{
    /// <summary>
    /// Represents a single user-defined shortcut — either a local .exe or a URL.
    /// </summary>
    public class ShortcutItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Label shown in the jump list menu.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Either a full path to an .exe (e.g. C:\Program Files\...\app.exe)
        /// or a URL (e.g. https://github.com).
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>Optional argument string passed when launching an exe.</summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>Sort order within the jump list.</summary>
        public int SortOrder { get; set; }

        public ShortcutType Type =>
            Target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            Target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? ShortcutType.Url
                : ShortcutType.Application;

        /// <summary>
        /// Returns the file icon for .exe targets, or a generic globe icon for URLs.
        /// </summary>
        public Image GetIcon()
        {
            if (Type == ShortcutType.Application && File.Exists(Target))
            {
                try
                {
                    var icon = Icon.ExtractAssociatedIcon(Target);
                    return icon?.ToBitmap();
                }
                catch { /* fallback below */ }
            }
            return null; // ContextMenuStrip will use default if null
        }
    }

    public enum ShortcutType
    {
        Application,
        Url
    }
}
