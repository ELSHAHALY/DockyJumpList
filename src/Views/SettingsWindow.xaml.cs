using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockyJumpList.Services;
using DockyJumpList.ViewModels;

namespace DockyJumpList.Views
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel _vm;

        // Drag-and-drop state
        private Point  _dragStartPoint;
        private bool   _isDragging;
        private object _draggedItem;

        public SettingsWindow(
            ShortcutService shortcutService,
            SettingsService settingsService,
            StartupService  startupService)
        {
            InitializeComponent();
            _vm = new SettingsViewModel(shortcutService, settingsService, startupService);
            DataContext = _vm;

            // Default active tab
            SetActiveTab("shortcuts");
        }

        // ── Tab switching ─────────────────────────────────────────

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                SetActiveTab(btn.Tag?.ToString());
        }

        private void SetActiveTab(string tab)
        {
            PanelShortcuts.Visibility = tab == "shortcuts" ? Visibility.Visible : Visibility.Collapsed;
            PanelPrefs.Visibility     = tab == "prefs"     ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Drag-and-drop reorder ─────────────────────────────────

        private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging     = false;
            _draggedItem    = null;
        }

        private void List_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos  = e.GetPosition(null);
            var diff = _dragStartPoint - pos;

            bool movedEnough =
                System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

            if (!movedEnough || _isDragging) return;

            // Find which list item is under the cursor
            var listBox = sender as ListBox;
            var item    = GetListBoxItemUnderMouse(listBox, e);
            if (item == null) return;

            _draggedItem = item.DataContext;
            _isDragging  = true;

            DragDrop.DoDragDrop(listBox, _draggedItem, DragDropEffects.Move);
        }

        private void List_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            _isDragging = false;
            if (_draggedItem == null) return;

            var listBox = sender as ListBox;
            var target  = GetListBoxItemUnderMouse(listBox, e)?.DataContext
                          as ViewModels.ShortcutItemViewModel;

            if (target == null || ReferenceEquals(target, _draggedItem)) return;

            _vm.MoveShortcut(
                _draggedItem as ViewModels.ShortcutItemViewModel,
                target);

            _draggedItem = null;
        }

        private static ListBoxItem GetListBoxItemUnderMouse(ListBox listBox, MouseEventArgs e)
        {
            var point = e.GetPosition(listBox);
            var hit   = listBox.InputHitTest(point) as DependencyObject;
            while (hit != null)
            {
                if (hit is ListBoxItem lbi) return lbi;
                hit = System.Windows.Media.VisualTreeHelper.GetParent(hit);
            }
            return null;
        }

        private static ListBoxItem GetListBoxItemUnderMouse(ListBox listBox, DragEventArgs e)
        {
            var point = e.GetPosition(listBox);
            var hit   = listBox.InputHitTest(point) as DependencyObject;
            while (hit != null)
            {
                if (hit is ListBoxItem lbi) return lbi;
                hit = System.Windows.Media.VisualTreeHelper.GetParent(hit);
            }
            return null;
        }
    }
}
