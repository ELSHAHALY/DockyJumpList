using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DockyJumpList.Models;
using DockyJumpList.Services;
using Microsoft.Win32;

namespace DockyJumpList.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ShortcutService  _shortcutService;
        private readonly SettingsService  _settingsService;
        private readonly StartupService   _startupService;
        private readonly IconCacheService _iconCache = new();

        public ObservableCollection<ShortcutItemViewModel> Shortcuts { get; } = new();

        // ── Selection ─────────────────────────────────────────────
        private ShortcutItemViewModel _selectedShortcut;
        public ShortcutItemViewModel SelectedShortcut
        {
            get => _selectedShortcut;
            set { _selectedShortcut = value; OnPropertyChanged(); }
        }

        // ── Form ──────────────────────────────────────────────────
        private bool  _isEditing;
        private Guid? _editingId;

        private string _formTitle = "NEW SHORTCUT";
        public string FormTitle   { get => _formTitle;     set { _formTitle     = value; OnPropertyChanged(); } }

        private string _formName = "";
        public string FormName    { get => _formName;      set { _formName      = value; OnPropertyChanged(); } }

        private string _formTarget = "";
        public string FormTarget  { get => _formTarget;    set { _formTarget    = value; OnPropertyChanged(); } }

        private string _formArguments = "";
        public string FormArguments { get => _formArguments; set { _formArguments = value; OnPropertyChanged(); } }

        // ── Preferences ───────────────────────────────────────────
        public bool LaunchOnStartup
        {
            get => _startupService.IsEnabled;
            set
            {
                _startupService.SetEnabled(value);
                _settingsService.Current.LaunchOnStartup = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        public bool ShowIconsInMenu
        {
            get => _settingsService.Current.ShowIconsInMenu;
            set
            {
                _settingsService.Current.ShowIconsInMenu = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        public string HotkeyLabel => _settingsService.Current.HotkeyDisplayLabel;

        // ── Commands ──────────────────────────────────────────────
        public ICommand NewShortcutCommand    { get; }
        public ICommand EditShortcutCommand   { get; }
        public ICommand RemoveShortcutCommand { get; }
        public ICommand SaveFormCommand       { get; }
        public ICommand CancelFormCommand     { get; }
        public ICommand BrowseCommand         { get; }

        public SettingsViewModel(
            ShortcutService shortcutService,
            SettingsService settingsService,
            StartupService  startupService)
        {
            _shortcutService = shortcutService;
            _settingsService = settingsService;
            _startupService  = startupService;

            NewShortcutCommand    = new RelayCommand(_ => StartNew());
            EditShortcutCommand   = new RelayCommand(_ => StartEdit(), _ => SelectedShortcut != null);
            RemoveShortcutCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedShortcut != null);
            SaveFormCommand       = new RelayCommand(_ => SaveForm(), _ => CanSave());
            CancelFormCommand     = new RelayCommand(_ => ClearForm());
            BrowseCommand         = new RelayCommand(_ => BrowseForExe());

            Reload();
        }

        // ── List management ───────────────────────────────────────

        private void Reload()
        {
            Shortcuts.Clear();
            foreach (var item in _shortcutService.GetAll())
            {
                var vm = new ShortcutItemViewModel(item);
                Shortcuts.Add(vm);
                LoadIconAsync(vm);
            }
        }

        private async void LoadIconAsync(ShortcutItemViewModel vm)
        {
            var source = await _iconCache.GetIconAsync(vm.Target);
            vm.IconSource = source;
        }

        // ── Form actions ──────────────────────────────────────────

        private void StartNew()
        {
            _isEditing = false; _editingId = null;
            FormTitle = "NEW SHORTCUT"; FormName = ""; FormTarget = ""; FormArguments = "";
            SelectedShortcut = null;
        }

        private void StartEdit()
        {
            if (SelectedShortcut == null) return;
            _isEditing    = true;
            _editingId    = SelectedShortcut.Id;
            FormTitle     = "EDIT SHORTCUT";
            FormName      = SelectedShortcut.DisplayName;
            FormTarget    = SelectedShortcut.Target;
            FormArguments = SelectedShortcut.Arguments;
        }

        private void RemoveSelected()
        {
            if (SelectedShortcut == null) return;
            var r = MessageBox.Show($"Remove '{SelectedShortcut.DisplayName}'?",
                "Docky Jump List", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;
            _shortcutService.Remove(SelectedShortcut.Id);
            Reload(); ClearForm();
        }

        private bool CanSave() =>
            !string.IsNullOrWhiteSpace(FormName) && !string.IsNullOrWhiteSpace(FormTarget);

        private void SaveForm()
        {
            var item = new ShortcutItem
            {
                Id          = _editingId ?? Guid.NewGuid(),
                DisplayName = FormName.Trim(),
                Target      = FormTarget.Trim(),
                Arguments   = FormArguments.Trim()
            };
            if (_isEditing) _shortcutService.Update(item);
            else            _shortcutService.Add(item);
            Reload(); ClearForm();
        }

        private void ClearForm()
        {
            _isEditing = false; _editingId = null;
            FormTitle = "NEW SHORTCUT"; FormName = ""; FormTarget = ""; FormArguments = "";
        }

        private void BrowseForExe()
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Select Application",
                Filter = "Applications (*.exe)|*.exe|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                FormTarget = dlg.FileName;
                if (string.IsNullOrWhiteSpace(FormName))
                    FormName = Path.GetFileNameWithoutExtension(dlg.FileName);
            }
        }

        // ── Drag-and-drop reorder ─────────────────────────────────

        public void MoveShortcut(ShortcutItemViewModel dragged, ShortcutItemViewModel target)
        {
            if (dragged == null || target == null) return;

            int from = Shortcuts.IndexOf(dragged);
            int to   = Shortcuts.IndexOf(target);
            if (from < 0 || to < 0 || from == to) return;

            Shortcuts.Move(from, to);

            var ordered = new List<ShortcutItem>();
            for (int i = 0; i < Shortcuts.Count; i++)
                ordered.Add(new ShortcutItem { Id = Shortcuts[i].Id, SortOrder = i });

            _shortcutService.Reorder(ordered);
        }

        // ── INotifyPropertyChanged ────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ── ShortcutItemViewModel ─────────────────────────────────────

    public class ShortcutItemViewModel : INotifyPropertyChanged
    {
        private readonly ShortcutItem _model;

        public Guid   Id          => _model.Id;
        public string DisplayName => _model.DisplayName;
        public string Target      => _model.Target;
        public string Arguments   => _model.Arguments;

        public string TypeIcon        => _model.Type == ShortcutType.Url ? "URL" : "APP";
        public string TypeBadgeColor  => _model.Type == ShortcutType.Url ? "#1A4D8A" : "#1A4D2E";

        private ImageSource _iconSource;
        public ImageSource IconSource
        {
            get => _iconSource;
            set { _iconSource = value; OnPropertyChanged(); }
        }

        public ShortcutItemViewModel(ShortcutItem model) => _model = model;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ── RelayCommand ──────────────────────────────────────────────

    public class RelayCommand : ICommand
    {
        private readonly Action<object>      _execute;
        private readonly Func<object, bool>  _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        { _execute = execute; _canExecute = canExecute; }

        public bool CanExecute(object p) => _canExecute?.Invoke(p) ?? true;
        public void Execute(object p)    => _execute(p);

        public event EventHandler CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
