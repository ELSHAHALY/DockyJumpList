# Docky Jump List

A lightweight Windows system-tray shortcut launcher built with C#, .NET Framework 4.8, and WPF.

Docky lives silently in your taskbar tray. Right-click its icon to instantly launch any app or website you've saved. No bloat, no window — just one icon and a fast menu.

---

## Features

- **Custom jump list** — right-click the tray icon to see all your shortcuts in a clean dark menu
- **Windows taskbar jump list** — right-click the pinned taskbar button to access your shortcuts natively through Windows
- **App & URL shortcuts** — supports local `.exe` files and `https://` web links
- **Global hotkey** — press `Ctrl + Alt + D` from anywhere to open the menu at your cursor
- **Settings panel** — left-click the tray icon to add, edit, remove, and reorder shortcuts
- **Drag-and-drop reorder** — drag rows in the settings list to set your preferred order
- **Real app icons** — automatically loads the icon from each `.exe` file
- **Launch on startup** — optional toggle to start Docky automatically with Windows
- **Single instance** — launching a second copy shows a notification instead of duplicating
- **Persistent storage** — shortcuts and preferences saved to `%AppData%\DockyJumpList`

---

## Requirements

| Requirement | Version |
|---|---|
| Windows | 10 or 11 |
| Visual Studio | 2022 or 2026 |
| Workload | .NET Desktop Development |
| .NET Framework | 4.8 |

No external NuGet packages required.

---

## Project Structure

```
DockyJumpList/
├── DockyJumpList.csproj
├── DockyJumpList.sln
├── app.manifest
├── README.md
│
├── src/
│   ├── App.xaml
│   ├── App.xaml.cs                         ← Entry point, owns tray icon lifetime
│   │
│   ├── Models/
│   │   ├── ShortcutItem.cs                 ← Core data model
│   │   └── AppSettings.cs                  ← User preferences model
│   │
│   ├── Services/
│   │   ├── ShortcutService.cs              ← CRUD + JSON persistence + launcher
│   │   ├── SettingsService.cs              ← Loads/saves AppSettings
│   │   ├── StartupService.cs               ← Windows Registry auto-launch
│   │   ├── SingleInstanceManager.cs        ← Mutex-based single instance guard
│   │   ├── GlobalHotkeyService.cs          ← Win32 RegisterHotKey wrapper
│   │   ├── IconCacheService.cs             ← Async .exe icon extraction + cache
│   │   ├── JumpListService.cs              ← Windows taskbar Jump List builder
│   │   └── DockyMenuRenderer.cs            ← Custom dark WinForms menu renderer
│   │
│   ├── ViewModels/
│   │   └── SettingsViewModel.cs            ← MVVM ViewModel + RelayCommand
│   │
│   ├── Views/
│   │   ├── SettingsWindow.xaml             ← Settings panel layout
│   │   └── SettingsWindow.xaml.cs          ← Drag-and-drop + tab switching
│   │
│   └── Resources/
│       ├── Styles.xaml                     ← Shared WPF dark theme styles
│       └── docky.ico                       ← Tray icon
│
└── docs/
    └── ARCHITECTURE.md                     ← Deep-dive design document
```

---

## Getting Started

### 1. Clone or download

```cmd
git clone https://github.com/yourname/DockyJumpList.git
cd DockyJumpList
```

Or download the ZIP and extract it anywhere.

### 2. Open in Visual Studio

Open `DockyJumpList.csproj` directly in Visual Studio 2022 or 2026. or the solution file.

### 3. Build and run

Press `F5`. No window will open — look for the Docky icon in your system tray (bottom-right, near the clock).

---

## Usage

| Action | Result |
|---|---|
| Right-click tray icon | Opens the jump list menu |
| Right-click taskbar button | Opens the native Windows Jump List |
| Left-click tray icon | Opens the Settings panel |
| `Ctrl + Alt + D` | Opens jump list at cursor position |
| Click a shortcut | Launches the app or opens the URL |
| Settings → + Add | Add a new shortcut |
| Settings → Edit | Edit the selected shortcut |
| Settings → Remove | Delete the selected shortcut |
| Drag a row | Reorder shortcuts |
| Preferences → Launch on startup | Toggle Windows auto-launch |

---

## Data Files

All data is stored per-user with no admin rights required:

```
%AppData%\DockyJumpList\
    shortcuts.json          ← Your saved shortcuts
    settings.json           ← Your preferences
    jumplist-urls\          ← Auto-generated .url shell-link files for URL shortcuts
                               (used by the Windows taskbar Jump List — do not edit manually)
```

You can back these up, share them between machines, or edit them manually in any text editor.

**shortcuts.json example:**
```json
[
  {
    "Id": "a3f2c1d0-...",
    "DisplayName": "VS Code",
    "Target": "C:\\Users\\You\\AppData\\Local\\Programs\\Microsoft VS Code\\Code.exe",
    "Arguments": "",
    "SortOrder": 0
  },
  {
    "Id": "b9e4f2a1-...",
    "DisplayName": "GitHub",
    "Target": "https://github.com",
    "Arguments": "",
    "SortOrder": 1
  }
]
```

---

## Changing the Hotkey

The default hotkey is `Ctrl + Alt + D`. To change it, open `%AppData%\DockyJumpList\settings.json` and update these two values, then restart Docky:

```json
{
  "HotkeyModifiers": 3,
  "HotkeyVirtualKey": 68,
  "HotkeyDisplayLabel": "Ctrl + Alt + D"
}
```

**Modifier values** (add them together for combinations):

| Modifier | Value |
|---|---|
| Alt | 1 |
| Ctrl | 2 |
| Shift | 4 |
| Win | 8 |

**Common virtual key codes:** A=65, B=66, ... D=68, ... Z=90. For a full list, search "Windows virtual key codes".

**Example — change to `Ctrl + Alt + J`:**
```json
"HotkeyModifiers": 3,
"HotkeyVirtualKey": 74,
"HotkeyDisplayLabel": "Ctrl + Alt + J"
```

---

## Building a Release Executable

```cmd
dotnet publish -c Release
```

Output: `bin\Release\net48\DockyJumpList.exe`

The published folder is self-contained — copy it anywhere and run the `.exe` directly.

---

## Troubleshooting

**The tray icon doesn't appear**
Make sure `src/Resources/docky.ico` exists and the build succeeded with no errors.

**Hotkey doesn't work**
Another application may already own `Ctrl + Alt + D`. A balloon notification will warn you on startup. Change the hotkey in `settings.json` as described above.

**"Already running" message on startup**
A previous instance is still in the tray. Right-click its icon and choose Exit, then launch again.

**Nullable error during build**
Make sure `DockyJumpList.csproj` contains `<LangVersion>latest</LangVersion>` and not `<Nullable>enable</Nullable>`.

**Icons not showing in the settings list**
Icons are loaded asynchronously — they appear within a second after the settings window opens. If an `.exe` path no longer exists, no icon is shown.

---

## Architecture

For a detailed breakdown of the service layer, MVVM pattern, data flow, and extension guide, see [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## License

MIT License. Free to use, modify, and distribute.