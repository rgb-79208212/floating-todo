using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloatingTodo
{
    public partial class MainWindow
    {
        private void OpenMoreMenu()
        {
            if (_menuPopup.IsOpen)
            {
                _menuPopup.IsOpen = false;
                return;
            }
            _taskMenuPopup.IsOpen = false;
            _duePopup.IsOpen = false;
            BuildMoreMenu();
            ConfigurePopup(_menuPopup, _moreButton, 6, true);
            _menuPopup.IsOpen = true;
        }

        private void BuildMoreMenu()
        {
            _moreMenuPanel.Children.Clear();
            _moreMenuPanel.Children.Add(CreateSwitchRow(
                LanguageManager.T("钉在桌面"), LanguageManager.T("锁定位置，防止误拖动"), _state.Settings.DesktopPinned,
                delegate { SetDesktopPinned(!_state.Settings.DesktopPinned); BuildMoreMenu(); }));
            _moreMenuPanel.Children.Add(CreateSwitchRow(
                LanguageManager.T("固定最上层"), LanguageManager.T("始终显示在其他窗口之上"), _state.Settings.AlwaysOnTop,
                delegate { SetAlwaysOnTop(!_state.Settings.AlwaysOnTop); BuildMoreMenu(); }));
            _moreMenuPanel.Children.Add(CreateSwitchRow(
                LanguageManager.T("开机自动显示"), LanguageManager.T("登录 Windows 后自动出现"), _state.Settings.AutoStart,
                delegate { SetAutoStart(!_state.Settings.AutoStart); BuildMoreMenu(); }));

            Border hair = new Border();
            hair.Height = 1;
            hair.Background = UiKit.Brush("#14FFFFFF");
            hair.Margin = new Thickness(0, 8, 0, 8);
            _moreMenuPanel.Children.Add(hair);

            Grid leadHead = new Grid();
            leadHead.Children.Add(UiKit.Text(LanguageManager.T("到期前标红"), 9.5, "#FFC7C1D2"));
            TextBlock leadValue = UiKit.Text(TaskService.LeadLabel(_state.Settings.LeadMinutes, LanguageManager.Current), 8.4, "#FF888294");
            leadValue.HorizontalAlignment = HorizontalAlignment.Right;
            leadHead.Children.Add(leadValue);
            _moreMenuPanel.Children.Add(leadHead);

            WrapPanel leadChips = new WrapPanel();
            leadChips.Margin = new Thickness(-2, 7, -2, 1);
            int[] values = TaskService.LeadMinuteOptions;
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                bool active = _state.Settings.LeadMinutes == value;
                Button chip = new Button();
                chip.Style = (Style)_root.Resources["ChipButtonStyle"];
                TextBlock label = UiKit.Text(TaskService.LeadLabel(value, LanguageManager.Current), 8.7, active ? "#FF19141F" : "#FFDCD6E8");
                if (active) label.FontWeight = FontWeights.SemiBold;
                chip.Content = label;
                chip.Margin = new Thickness(2);
                chip.Padding = new Thickness(7, 4, 7, 4);
                if (active)
                {
                    chip.Background = UiKit.Brush("#D9BCA9FF");
                    chip.BorderBrush = UiKit.Brush("#55D4C7FF");
                }
                chip.Click += delegate
                {
                    _state.Settings.LeadMinutes = value;
                    _store.SaveAsync();
                    RebuildAll(true);
                    BuildMoreMenu();
                };
                AutomationProperties.SetName(chip, LanguageManager.Format("到期前 {0} 标红",
                    TaskService.LeadLabel(value, LanguageManager.Current)));
                leadChips.Children.Add(chip);
            }
            _moreMenuPanel.Children.Add(leadChips);

            TextBlock sizeLabel = UiKit.Text(LanguageManager.T("窗口大小"), 9.2, "#FF9A93A8");
            sizeLabel.Margin = new Thickness(4, 8, 0, 4);
            _moreMenuPanel.Children.Add(sizeLabel);
            WrapPanel sizeChips = new WrapPanel();
            sizeChips.Margin = new Thickness(-2, 0, -2, 2);
            AddSizeChip(sizeChips, LanguageManager.T("紧凑"), 340, 500);
            AddSizeChip(sizeChips, LanguageManager.T("标准"), 372, 566);
            AddSizeChip(sizeChips, LanguageManager.T("宽屏"), 440, 650);
            _moreMenuPanel.Children.Add(sizeChips);

            TextBlock langLabel = UiKit.Text(LanguageManager.T("语言"), 9.2, "#FF9A93A8");
            langLabel.Margin = new Thickness(4, 8, 0, 4);
            _moreMenuPanel.Children.Add(langLabel);
            WrapPanel langChips = new WrapPanel();
            langChips.Margin = new Thickness(-2, 0, -2, 2);
            AddLanguageChip(langChips, "中文", AppLanguage.Chinese);
            AddLanguageChip(langChips, "English", AppLanguage.English);
            _moreMenuPanel.Children.Add(langChips);

            if (!string.IsNullOrEmpty(_autoStartError))
            {
                TextBlock error = UiKit.Text(_autoStartError, 8.2, "#FFFF9AA3");
                error.TextWrapping = TextWrapping.Wrap;
                error.Margin = new Thickness(4, 4, 4, 2);
                _moreMenuPanel.Children.Add(error);
            }

        }

        private void AddSizeChip(WrapPanel panel, string label, double width, double height)
        {
            Button chip = new Button();
            chip.Style = (Style)_root.Resources["ChipButtonStyle"];
            chip.Content = UiKit.Text(label, 8.7, "#FFDCD6E8");
            chip.Margin = new Thickness(2);
            chip.Padding = new Thickness(7, 4, 7, 4);
            chip.Click += delegate
            {
                SetWindowSize(width, height);
                BuildMoreMenu();
            };
            AutomationProperties.SetName(chip, LanguageManager.Format("窗口大小 {0}", label));
            panel.Children.Add(chip);
        }

        private void AddLanguageChip(WrapPanel panel, string label, AppLanguage language)
        {
            bool active = LanguageManager.Current == language;
            Button chip = new Button();
            chip.Style = (Style)_root.Resources["ChipButtonStyle"];
            TextBlock text = UiKit.Text(label, 8.7, active ? "#FF19141F" : "#FFDCD6E8");
            if (active) text.FontWeight = FontWeights.SemiBold;
            chip.Content = text;
            chip.Margin = new Thickness(2);
            chip.Padding = new Thickness(7, 4, 7, 4);
            if (active)
            {
                chip.Background = UiKit.Brush("#D9BCA9FF");
                chip.BorderBrush = UiKit.Brush("#55D4C7FF");
            }
            chip.Click += delegate { SetLanguage(language); };
            AutomationProperties.SetName(chip, LanguageManager.T("语言") + ": " + label);
            panel.Children.Add(chip);
        }

        private Button CreateSwitchRow(string title, string subtitle, bool on, RoutedEventHandler click)
        {
            Button button = new Button();
            button.Style = (Style)_root.Resources["MenuActionStyle"];
            button.Height = 42;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            Grid content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel labels = new StackPanel();
            labels.VerticalAlignment = VerticalAlignment.Center;
            TextBlock titleBlock = UiKit.Text(title, 9.9, "#FFF1EDF7");
            labels.Children.Add(titleBlock);
            TextBlock sub = UiKit.Text(subtitle, 8.1, "#FF8A8496");
            sub.Margin = new Thickness(0, 1, 0, 0);
            labels.Children.Add(sub);
            Grid.SetColumn(labels, 0);
            content.Children.Add(labels);
            Border toggle = UiKit.MakeSwitch(on);
            Grid.SetColumn(toggle, 1);
            content.Children.Add(toggle);
            button.Content = content;
            button.Click += click;
            AutomationProperties.SetName(button, title);
            return button;
        }

        private Button CreateMenuActionRow(FrameworkElement icon, string text, bool danger)
        {
            Button button = new Button();
            button.Style = (Style)_root.Resources["MenuActionStyle"];
            button.Height = 33;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            Grid content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            icon.Margin = new Thickness(0, 0, 8, 0);
            icon.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(icon, 0);
            content.Children.Add(icon);
            TextBlock label = UiKit.Text(text, 9.8, danger ? "#FFFF9AA3" : "#FFE3DEEE");
            Grid.SetColumn(label, 1);
            content.Children.Add(label);
            button.Content = content;
            AutomationProperties.SetName(button, text);
            return button;
        }

        private void OpenTaskMenu(TodoTask task, UIElement target)
        {
            if (task == null) return;
            _contextTask = task;
            _contextAnchor = target;
            _menuPopup.IsOpen = false;
            _duePopup.IsOpen = false;
            BuildTaskMenu(task);
            ConfigurePopup(_taskMenuPopup, target as FrameworkElement, 4, true);
            _taskMenuPopup.IsOpen = true;
        }

        private void BuildTaskMenu(TodoTask task)
        {
            _taskMenuPanel.Children.Clear();
            TextBlock caption = UiKit.Text(Shorten(task.Title, 21), 8.8, "#FF8B8597");
            caption.Margin = new Thickness(7, 2, 7, 5);
            caption.TextTrimming = TextTrimming.CharacterEllipsis;
            _taskMenuPanel.Children.Add(caption);
            Border hair = new Border();
            hair.Height = 1;
            hair.Background = UiKit.Brush("#14FFFFFF");
            hair.Margin = new Thickness(0, 0, 0, 4);
            _taskMenuPanel.Children.Add(hair);

            TaskRowView view;
            _rowViews.TryGetValue(task.Id, out view);
            Button edit = CreateSmallActionRow(
                UiKit.Icon("M3,13 L4,9 L10.5,2.5 C11.2,1.8 12.3,1.8 13,2.5 C13.7,3.2 13.7,4.3 13,5 L6.5,11.5 Z M3,13 L6.5,11.5",
                    13, "#FFD8D2E4", 1.15), LanguageManager.T("编辑内容"), false);
            edit.Click += delegate
            {
                _taskMenuPopup.IsOpen = false;
                OpenTaskEditor(task);
            };
            _taskMenuPanel.Children.Add(edit);

            if (!task.Done)
            {
                Button due = CreateSmallActionRow(
                    UiKit.ClockIcon(13, "#FFD8D2E4", 1.15),
                    task.DueAt.HasValue ? LanguageManager.T("更改截止时间") : LanguageManager.T("设置截止时间"), false);
                due.Click += delegate
                {
                    UIElement anchor = _contextAnchor;
                    _taskMenuPopup.IsOpen = false;
                    Dispatcher.BeginInvoke(new Action(delegate { OpenDueEditorForTask(task, anchor); }),
                        DispatcherPriority.Background);
                };
                _taskMenuPanel.Children.Add(due);

                if (task.DueAt.HasValue)
                {
                    Button clear = CreateSmallActionRow(
                        UiKit.Icon("M3.2,8.2 L12.8,8.2", 12, "#FFAAA4B6", 1.25), LanguageManager.T("清除截止时间"), false);
                    clear.Click += delegate
                    {
                        _taskMenuPopup.IsOpen = false;
                        task.DueAt = null;
                        _store.SaveAsync();
                        RebuildAll(true);
                    };
                    _taskMenuPanel.Children.Add(clear);
                }

                Button complete = CreateSmallActionRow(
                    UiKit.CheckIcon(13.5, "#FFD8D2E4", 1.35), LanguageManager.T("标记为已完成"), false);
                complete.Click += delegate { _taskMenuPopup.IsOpen = false; ToggleDone(task); };
                _taskMenuPanel.Children.Add(complete);
            }
            else
            {
                Button restore = CreateSmallActionRow(
                    UiKit.Icon("M3,8 A5,5 0 1 1 5,12 M3,8 L3,4 M3,8 L7,8", 13.5, "#FFD8D2E4", 1.25),
                    LanguageManager.T("恢复为待办"), false);
                restore.Click += delegate { _taskMenuPopup.IsOpen = false; ToggleDone(task); };
                _taskMenuPanel.Children.Add(restore);
            }

            Border hair2 = new Border();
            hair2.Height = 1;
            hair2.Background = UiKit.Brush("#14FFFFFF");
            hair2.Margin = new Thickness(0, 5, 0, 4);
            _taskMenuPanel.Children.Add(hair2);
            Button delete = CreateSmallActionRow(
                UiKit.Icon("M3,4.7 L13,4.7 M6,4.7 L6,2.8 L10,2.8 L10,4.7 M4.4,4.7 L5.2,13.4 L10.8,13.4 L11.6,4.7 M7,7.1 L7,11.2 M9,7.1 L9,11.2",
                    13, "#FFFF98A2", 1.2), LanguageManager.T("删除待办…"), true);
            delete.Click += delegate { _taskMenuPopup.IsOpen = false; ConfirmDeleteTask(task); };
            _taskMenuPanel.Children.Add(delete);
        }

        private Button CreateSmallActionRow(FrameworkElement icon, string text, bool danger)
        {
            Button button = new Button();
            button.Style = (Style)_root.Resources["MenuActionStyle"];
            button.Height = 32;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            Grid content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            icon.Margin = new Thickness(0, 0, 8, 0);
            icon.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(icon, 0);
            content.Children.Add(icon);
            TextBlock label = UiKit.Text(text, 9.6, danger ? "#FFFF9AA3" : "#FFE8E3F0");
            Grid.SetColumn(label, 1);
            content.Children.Add(label);
            button.Content = content;
            AutomationProperties.SetName(button, text);
            return button;
        }

        private void ConfigurePopup(Popup popup, FrameworkElement target, double gap, bool alignRight)
        {
            popup.PlacementTarget = target;
            popup.Placement = PlacementMode.Custom;
            popup.HorizontalOffset = 0;
            popup.VerticalOffset = 0;
            popup.CustomPopupPlacementCallback = delegate(Size popupSize, Size targetSize, Point offset)
            {
                Point screen = target.PointToScreen(new Point(0, 0));
                double dpiX = 1;
                double dpiY = 1;
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                {
                    Matrix m = source.CompositionTarget.TransformToDevice;
                    dpiX = m.M11 <= 0 ? 1 : m.M11;
                    dpiY = m.M22 <= 0 ? 1 : m.M22;
                }
                double sx = screen.X / dpiX;
                double sy = screen.Y / dpiY;
                Rect work = SystemParameters.WorkArea;
                double desiredLeft = alignRight ? sx + targetSize.Width - popupSize.Width : sx;
                desiredLeft = Math.Max(work.Left + 8, Math.Min(desiredLeft, work.Right - popupSize.Width - 8));
                double relX = desiredLeft - sx;
                double belowY = targetSize.Height + gap;
                double aboveY = -popupSize.Height - gap;
                bool fitsBelow = sy + belowY + popupSize.Height <= work.Bottom - 8;
                CustomPopupPlacement below = new CustomPopupPlacement(new Point(relX, belowY), PopupPrimaryAxis.None);
                CustomPopupPlacement above = new CustomPopupPlacement(new Point(relX, aboveY), PopupPrimaryAxis.None);
                return fitsBelow ? new CustomPopupPlacement[] { below, above }
                    : new CustomPopupPlacement[] { above, below };
            };
        }

        private static string Shorten(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= max ? value : value.Substring(0, max) + "…";
        }
    }
}
