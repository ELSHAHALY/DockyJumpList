using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Extracts and caches icons for .exe files asynchronously.
    /// Icons are cached by file path to avoid repeated disk hits.
    /// Returns a WPF ImageSource suitable for binding in the settings list.
    /// </summary>
    public class IconCacheService
    {
        private readonly ConcurrentDictionary<string, ImageSource> _cache = new();

        /// <summary>
        /// Returns a cached ImageSource for the given exe path.
        /// Falls back to a generic icon if extraction fails or path is a URL.
        /// </summary>
        public async Task<ImageSource> GetIconAsync(string target)
        {
            if (_cache.TryGetValue(target, out var cached))
                return cached;

            var source = await Task.Run(() => ExtractIcon(target));

            if (source != null)
                _cache[target] = source;

            return source;
        }

        private ImageSource ExtractIcon(string target)
        {
            // URLs don't have a file icon — return null (caller shows fallback)
            if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!File.Exists(target))
                return null;

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(target);
                if (icon == null) return null;

                using var bitmap = icon.ToBitmap();
                return BitmapToImageSource(bitmap);
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new System.IO.MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption  = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Make it cross-thread safe
            return bitmapImage;
        }

        public void Clear() => _cache.Clear();
    }
}
