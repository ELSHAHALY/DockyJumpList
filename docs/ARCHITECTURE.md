# Docky Jump List — Project Architecture & Developer Guide

**Version:** 1.0.0  
**Stack:** C# · .NET 4.8 · WPF · Windows Forms (tray only)  
**Author:** Abdallah W. Elshahaly  

---

## 1. Project Overview

Docky Jump List is a lightweight Windows desktop utility that lives in the system tray and gives the user instant keyboard-free access to their most-used applications and web links. It replaces a cluttered taskbar with a single icon and a clean, fast jump list menu.

---

## 2. Folder Structure

```
DockyJumpList/
├── DockyJumpList.csproj          ← SDK-style .NET 4.8 project
├── app.manifest                  ← Per-monitor DPI awareness
│
└── src/
    ├── App.xaml                  ← Entry point, ShutdownMode=OnExplicitShutdown
    ├── App.xaml.cs               ← Bootstraps tray icon, wires ShortcutService
    ├── StartupNote.cs            ← Documentation note (no StartupUri in App.xaml)
    │
    ├── Models/
    │   └── ShortcutItem.cs       ← Core data model (Id, DisplayName, Target, Arguments, Type)
    │
    ├── Services/
    │   ├── ShortcutService.cs    ← CRUD + JSON persistence + Process.Start launcher
    │   └── DockyMenuRenderer.cs  ← Custom WinForms dark-theme ContextMenuStrip renderer
    │
    ├── ViewModels/
    │   └── SettingsViewModel.cs  ← MVVM ViewModel + RelayCommand + ShortcutItemViewModel
    │
    ├── Views/
    │   ├── SettingsWindow.xaml   ← Settings panel layout (dark WPF)
    │   └── SettingsWindow.xaml.cs
    │
    └── Resources/
        ├── Styles.xaml           ← Shared WPF control styles (InputBox, PrimaryButton, etc.)
        └── docky.ico             ← Tray icon (you must supply this file)
```

---

## 3. Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                       App.xaml.cs                       │
│   (Application entry — owns NotifyIcon lifetime)        │
│                                                         │
│  OnStartup()                                            │
│    ├─ ShortcutService.Load()  ← reads JSON from disk    │
│    └─ InitializeTrayIcon()                              │
│         ├─ Left-click  → OpenSettingsWindow()           │
│         └─ Right-click → BuildContextMenu()             │
└───────────────────┬─────────────────────────────────────┘
                    │ ShortcutsChanged event
        ┌───────────▼────────────┐
        │    ShortcutService     │   ← pure business logic
        │  GetAll / Add / Update │
        │  Remove / Reorder      │
        │  Launch (Process.Start)│
        │  Load / Save (JSON)    │
        └───────────┬────────────┘
                    │
        ┌───────────▼──────────────────┐
        │       SettingsWindow         │
        │  DataContext = SettingsVM    │
        │                              │
        │  SettingsViewModel           │
        │  ├─ ObservableCollection<VM> │
        │  ├─ Form fields (INotify)    │
        │  └─ RelayCommands            │
        └──────────────────────────────┘
```

### Key design decisions

| Decision | Rationale |
|---|---|
| `ShutdownMode = OnExplicitShutdown` | WPF would auto-exit when the settings window closes. Tray apps must own their own lifetime. |
| `ShortcutService` raises `ShortcutsChanged` event | Decouples tray menu rebuild from ViewModel. App.xaml subscribes directly. |
| WinForms `NotifyIcon` inside a WPF app | WPF has no built-in tray icon. WinForms `System.Windows.Forms` is a proven solution supported on .NET 4.8. |
| Custom `DockyMenuRenderer` | Default WinForms context menu is light-themed. The renderer gives it a dark, branded look. |
| JSON persistence in `%AppData%\DockyJumpList` | Per-user, no admin rights needed. Human-readable and easy to back up. |
| `Process.Start` with `UseShellExecute = true` | Handles both `.exe` paths and `https://` URLs with a single call pattern. |

---

## 4. Data Model

```csharp
public class ShortcutItem
{
    public Guid   Id          { get; set; }  // Stable identity across renames
    public string DisplayName { get; set; }  // Label in jump list
    public string Target      { get; set; }  // Full .exe path OR https:// URL
    public string Arguments   { get; set; }  // Optional CLI args (exe only)
    public int    SortOrder   { get; set; }  // User-defined position

    public ShortcutType Type { get; }        // Derived: Application | Url
}
```

**Persistence format** (`%AppData%\DockyJumpList\shortcuts.json`):

```json
[
  {
    "Id": "a3f2...",
    "DisplayName": "VS Code",
    "Target": "C:\\Users\\...\\Code.exe",
    "Arguments": "",
    "SortOrder": 0
  },
  {
    "Id": "c9d1...",
    "DisplayName": "GitHub",
    "Target": "https://github.com",
    "Arguments": "",
    "SortOrder": 1
  }
]
```

---

## 5. MVVM Pattern

The Settings panel follows strict MVVM:

```
View  (SettingsWindow.xaml)
  └── binds to ──►  SettingsViewModel  (INotifyPropertyChanged)
                         └── calls ──►  ShortcutService  (plain C# service)
```

- Views never reference `ShortcutService` directly.
- Commands (`RelayCommand`) wrap all user actions.
- `ObservableCollection<ShortcutItemViewModel>` drives the `ListBox` — UI updates automatically when items are added/removed.

---

## 6. Build & Run Instructions

### Prerequisites
- Visual Studio 2022 (Community or higher)
- .NET Desktop Development workload
- Windows 10 or 11

### Steps

1. Clone or download the project.
2. Place a 16x16 and 32x32 `.ico` file at `src/Resources/docky.ico`.
3. Open `DockyJumpList.csproj` in Visual Studio.
4. Build → Run (F5).
5. The app will appear in the system tray — no window opens on launch.

### Release build

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Output in `bin/Release/net48/win-x64/publish/`.

---

## 7. Extending the Project

### Add a new shortcut type (e.g. PowerShell script)

1. Add `Script` to the `ShortcutType` enum in `ShortcutItem.cs`.
2. Update `ShortcutItem.Type` getter to detect `.ps1` extensions.
3. Update `ShortcutService.Launch()` to call `powershell.exe -File <path>`.
4. Update `ShortcutItemViewModel.TypeIcon` and `TypeBadgeColor`.

### Add startup with Windows

```csharp
// In ShortcutService or a StartupService:
Registry.CurrentUser
    .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
    .SetValue("DockyJumpList", Application.ExecutablePath);
```

### Add drag-and-drop reordering in Settings

Use `PreviewMouseMove` and `DragDrop` on the `ListBox`, then call `_service.Reorder(newList)`.

---

## 8. Known Limitations & Future Work

| Item | Notes |
|---|---|
| No hotkey support | Could add global keyboard hook (WinAPI `SetWindowsHookEx`) in v1.1 |
| No icon preview in settings list | Fetching `.exe` icons asynchronously would improve UX |
| No import/export | Exporting `shortcuts.json` manually works, but a UI button would help |
| Single list only | Could support named "groups" with sub-menus |
| No auto-launch on Windows startup | Can be added via Registry write (see §7) |

---

## 9. File Checklist Before First Build

- [ ] `src/Resources/docky.ico` exists (create a 16/32px icon in any tool)
- [ ] `.csproj` references match actual file paths
- [ ] `App.xaml` has no `StartupUri` attribute (tray apps must not have a startup window)
- [ ] Target framework in `.csproj` matches your installed .NET (`net48` or `net6.0-windows`)
