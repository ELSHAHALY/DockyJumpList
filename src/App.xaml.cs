using DockyJumpList.Services;
using DockyJumpList.src.Services;
using DockyJumpList.Views;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace DockyJumpList {
    public partial class App : Application {
        private SingleInstanceManager _singleInstance;
        private NotifyIcon _trayIcon;
        private ShortcutService _shortcutService;
        private SettingsService _settingsService;
        private StartupService _startupService;
        private GlobalHotkeyService _hotkeyService;
        private IconCacheService _iconCache;
        private JumpListService _jumpListService;
        private SettingsWindow _settingsWindow;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            _singleInstance = new SingleInstanceManager();
            if (!_singleInstance.Acquire()) {
                System.Windows.MessageBox.Show(
                    "Docky Jump List is already running.\nCheck your system tray.",
                    "Docky Jump List",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            _settingsService = new SettingsService();
            _shortcutService = new ShortcutService();
            _startupService = new StartupService();
            _iconCache = new IconCacheService();
            _jumpListService = new JumpListService();

            _shortcutService.Load();
            _jumpListService.Refresh(_shortcutService.GetAll());

            InitializeTrayIcon();
            RegisterHotkey();
        }

        private void InitializeTrayIcon() {
            _trayIcon = new NotifyIcon {
                Icon = LoadTrayIcon(),
                Text = "Docky Jump List  (Ctrl+Alt+D)",
                Visible = true
            };

            _trayIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left)
                    OpenSettingsWindow();
            };

            _trayIcon.ContextMenuStrip = BuildContextMenu();

            _shortcutService.ShortcutsChanged += () => {
                _iconCache.Clear();
                _trayIcon.ContextMenuStrip?.Dispose();
                _trayIcon.ContextMenuStrip = BuildContextMenu();
                _jumpListService.Refresh(_shortcutService.GetAll());
            };
        }

        private ContextMenuStrip BuildContextMenu() {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI", 9.5f);
            menu.Renderer = new DockyMenuRenderer();

            var shortcuts = _shortcutService.GetAll();

            if (shortcuts.Count == 0) {
                menu.Items.Add(new ToolStripMenuItem("No shortcuts yet — click to add some") { Enabled = false });
            }
            else {
                foreach (var sc in shortcuts) {
                    var item = new ToolStripMenuItem(sc.DisplayName) { Tag = sc };

                    if (_settingsService.Current.ShowIconsInMenu) {
                        _ = _iconCache.GetIconAsync(sc.Target).ContinueWith(t => {
                            if (t.Result == null) return;
                            var bmp = ImageSourceToBitmap(t.Result);
                            if (bmp == null) return;
                            System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                                if (!item.IsDisposed) item.Image = bmp;
                            });
                        });
                    }

                    item.Click += (s, e) => _shortcutService.Launch(sc);
                    menu.Items.Add(item);
                }
            }

            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => OpenSettingsWindow();
            menu.Items.Add(settingsItem);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitApp();
            menu.Items.Add(exitItem);

            return menu;
        }

        private void RegisterHotkey() {
            var prefs = _settingsService.Current;
            _hotkeyService = new GlobalHotkeyService();
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;

            bool ok = _hotkeyService.Register(prefs.HotkeyModifiers, prefs.HotkeyVirtualKey);

            if (!ok) {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "Docky Jump List",
                    $"Could not register hotkey {prefs.HotkeyDisplayLabel}. Another app may be using it.",
                    ToolTipIcon.Warning);
            }
        }

        private void OnHotkeyPressed() {
            var pos = System.Windows.Forms.Cursor.Position;
            _trayIcon.ContextMenuStrip?.Show(pos);
        }

        private void OpenSettingsWindow() {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded) {
                _settingsWindow = new SettingsWindow(
                    _shortcutService,
                    _settingsService,
                    _startupService);
                _settingsWindow.Show();
            }
            else {
                _settingsWindow.Activate();
                _settingsWindow.WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// Loads the tray icon with three fallback layers:
        ///
        ///  1. Pack URI — reads the icon from the WPF embedded resource stream.
        ///     This is the correct approach when the .ico is declared as
        ///     &lt;Resource&gt; in the .csproj (packed into the assembly at build time).
        ///
        ///  2. File system — looks for Resources\docky.ico next to the .exe.
        ///     Useful during development if you run directly from the output folder.
        ///
        ///  3. SystemIcons.Application — guaranteed fallback so the tray icon
        ///     always appears even if both of the above fail.
        /// </summary>
        private Icon LoadTrayIcon() {
            // 1. Embedded WPF resource (pack URI)
            try {
                var sri = Application.GetResourceStream(
                    new Uri("pack://application:,,,/src/Resources/docky.ico"));

                if (sri?.Stream != null) {
                    // Copy to MemoryStream — pack streams are forward-only,
                    // but Icon(Stream) needs a seekable stream.
                    using var ms = new MemoryStream();
                    sri.Stream.CopyTo(ms);
                    ms.Position = 0;
                    return new Icon(ms);
                }
            }
            catch { /* fall through */ }

            // 2. File next to the executable
            try {
                var iconPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "docky.ico");

                if (File.Exists(iconPath))
                    return new Icon(iconPath);
            }
            catch { /* fall through */ }

            // 3. Built-in Windows fallback
            return SystemIcons.Application;
        }

        private static System.Drawing.Image ImageSourceToBitmap(System.Windows.Media.ImageSource source) {
            try {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(
                    (System.Windows.Media.Imaging.BitmapSource)source));
                using var stream = new MemoryStream();
                encoder.Save(stream);
                stream.Position = 0;
                return System.Drawing.Image.FromStream(stream);
            }
            catch { return null; }
        }

        private void ExitApp() {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _hotkeyService?.Dispose();
            _singleInstance?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e) {
            _trayIcon?.Dispose();
            _hotkeyService?.Dispose();
            _singleInstance?.Dispose();
            base.OnExit(e);
        }
    }
}