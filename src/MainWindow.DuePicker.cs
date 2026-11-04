using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FloatingTodo
{
    public partial class MainWindow
    {
        private void OpenDueEditorForPending()
        {
            bool hadDue = _pendingDue.HasValue;
            DateTime initial = _pendingDue.HasValue ? _pendingDue.Value : DefaultDueTime();
            OpenDueEditor(_dueButton, initial, hadDue, delegate(DateTime? value)
            {
                _pendingDue = value;
                UpdatePendingDueUi();
                if (_inputBox != null && string.IsNullOrEmpty(_inputBox.Text)) FocusInput();
            });
        }

        private void OpenDueEditorForTask(TodoTask task, UIElement anchor)
        {
            if (task == null) return;
            DateTime initial = task.DueAt.HasValue ? task.DueAt.Value : DefaultDueTime();
            UIElement target = anchor;
            if (!(target is FrameworkElement))
            {
                TaskRowView view;
                if (_rowViews.TryGetValue(task.Id, out view)) target = view.Row;
            }
            FrameworkElement actual = target as FrameworkElement;
            if (actual == null) actual = _dueButton;
            OpenDueEditor(actual, initial, task.DueAt.HasValue, delegate(DateTime? value)
            {
                task.DueAt = value;
                _store.SaveAsync();
                RebuildAll(true);
                UpdateTrayState();
            });
        }

        private void OpenDueEditorForSelection()
        {
            List<TodoTask> selected = GetSelectedTasks();
            List<TodoTask> active = new List<TodoTask>();
            DateTime? earliest = null;
            bool allowClear = false;
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i].Done) continue;
                active.Add(selected[i]);
                if (selected[i].DueAt.HasValue)
                {
                    allowClear = true;
                    if (!earliest.HasValue || selected[i].DueAt.Value < earliest.Value) earliest = selected[i].DueAt;
                }
            }
            if (active.Count == 0) return;
            DateTime initial = earliest.HasValue ? earliest.Value : DefaultDueTime();
            OpenDueEditor(_batchDueButton, initial, allowClear, delegate(DateTime? value)
            {
                TaskService.SetDueAll(active, value);
                _store.SaveAsync();
                _selectedTaskIds.Clear();
                _selectionMode = false;
                _normalFooter.Visibility = Visibility.Visible;
                _batchBar.Visibility = Visibility.Collapsed;
                _selectModeText.Text = "选择";
                RebuildAll(true);
                UpdateTrayState();
            });
        }

        private void OpenDueEditor(FrameworkElement anchor, DateTime initial, bool allowClear, Action<DateTime?> apply)
        {
            _menuPopup.IsOpen = false;
            _taskMenuPopup.IsOpen = false;
            _dueApply = apply;
            _dueAnchor = anchor;
            _dueAllowClear = allowClear;
            _selectedDate = initial.Date;
            _calendarMonth = new DateTime(initial.Year, initial.Month, 1);
            _pickerYear = initial.Year;
            _yearRangeStart = _pickerYear - (_pickerYear % 12);
            _selectedHour = initial.Hour;
            _selectedMinute = initial.Minute;
            _monthYearMode = false;
            _dueErrorText.Visibility = Visibility.Collapsed;
            _dueClearButton.Visibility = allowClear ? Visibility.Visible : Visibility.Collapsed;
            _dueMonthLabel.Text = _calendarMonth.ToString("yyyy年M月");
            _duePrevButton.Visibility = Visibility.Visible;
            _dueNextButton.Visibility = Visibility.Visible;
            _dueCalendarArea.Visibility = Visibility.Visible;
            _dueYearMonthPanel.Visibility = Visibility.Collapsed;
            BuildCalendar();
            UpdateTimeFields();
            HideTimeDropdown();
            ConfigurePopup(_duePopup, anchor, 7, true);
            _duePopup.IsOpen = true;
        }

        private DateTime DefaultDueTime()
        {
            DateTime now = DateTime.Now.AddHours(1);
            int minute = now.Minute;
            int add = 5 - (minute % 5);
            if (add == 5) add = 5;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0).AddMinutes(add);
        }

        private void ToggleMonthYearMode()
        {
            _monthYearMode = !_monthYearMode;
            if (_monthYearMode)
            {
                _pickerYear = _calendarMonth.Year;
                _yearRangeStart = _pickerYear - (_pickerYear % 12);
                BuildYearMonthGrids();
                _dueCalendarArea.Visibility = Visibility.Collapsed;
                _dueYearMonthPanel.Visibility = Visibility.Visible;
                _duePrevButton.Visibility = Visibility.Collapsed;
                _dueNextButton.Visibility = Visibility.Collapsed;
                _dueMonthLabel.Text = "返回日历";
            }
            else
            {
                _dueCalendarArea.Visibility = Visibility.Visible;
                _dueYearMonthPanel.Visibility = Visibility.Collapsed;
                _duePrevButton.Visibility = Visibility.Visible;
                _dueNextButton.Visibility = Visibility.Visible;
                _dueMonthLabel.Text = _calendarMonth.ToString("yyyy年M月");
            }
        }

        private void BuildYearMonthGrids()
        {
            if (_dueYearRangeStart != _yearRangeStart) _dueYearRangeStart = _yearRangeStart;
            _dueYearRangeLabel.Text = _yearRangeStart.ToString() + " – " + (_yearRangeStart + 11).ToString();
            _dueYearGrid.Children.Clear();
            for (int i = 0; i < 12; i++)
            {
                int year = _yearRangeStart + i;
                Button button = new Button();
                button.Style = (Style)_root.Resources["CalendarDayStyle"];
                button.Height = 25;
                button.Margin = new Thickness(2);
                bool selected = year == _pickerYear;
                TextBlock label = UiKit.Text(year.ToString(), 9.2, selected ? "#FF19141F" : "#FFDCD7E8");
                if (selected) label.FontWeight = FontWeights.SemiBold;
                button.Content = label;
                if (selected) button.Background = UiKit.Brush("#E0BCA9FF");
                button.Click += delegate
                {
                    _pickerYear = year;
                    BuildYearMonthGrids();
                };
                AutomationProperties.SetName(button, year.ToString() + "年");
                _dueYearGrid.Children.Add(button);
            }

            _dueMonthGrid.Children.Clear();
            for (int i = 1; i <= 12; i++)
            {
                int month = i;
                bool selected = _pickerYear == _calendarMonth.Year && month == _calendarMonth.Month;
                Button button = new Button();
                button.Style = (Style)_root.Resources["CalendarDayStyle"];
                button.Height = 25;
                button.Margin = new Thickness(2);
                TextBlock label = UiKit.Text(month.ToString() + "月", 9.2, selected ? "#FF19141F" : "#FFDCD7E8");
                if (selected) label.FontWeight = FontWeights.SemiBold;
                button.Content = label;
                if (selected) button.Background = UiKit.Brush("#E0BCA9FF");
                button.Click += delegate
                {
                    int day = _selectedDate.HasValue ? Math.Min(_selectedDate.Value.Day, DateTime.DaysInMonth(_pickerYear, month)) : 1;
                    _selectedDate = new DateTime(_pickerYear, month, Math.Max(1, day));
                    _calendarMonth = new DateTime(_pickerYear, month, 1);
                    _monthYearMode = false;
                    _dueCalendarArea.Visibility = Visibility.Visible;
                    _dueYearMonthPanel.Visibility = Visibility.Collapsed;
                    _duePrevButton.Visibility = Visibility.Visible;
                    _dueNextButton.Visibility = Visibility.Visible;
                    _dueMonthLabel.Text = _calendarMonth.ToString("yyyy年M月");
                    BuildCalendar();
                };
                AutomationProperties.SetName(button, month.ToString() + "月");
                _dueMonthGrid.Children.Add(button);
            }
        }

        private int _dueYearRangeStart = int.MinValue;

        private void BuildCalendar()
        {
            _dueDayGrid.Children.Clear();
            _dueMonthLabel.Text = _calendarMonth.ToString("yyyy年M月");
            int leading = ((int)_calendarMonth.DayOfWeek + 6) % 7;
            int days = DateTime.DaysInMonth(_calendarMonth.Year, _calendarMonth.Month);
            DateTime today = DateTime.Now.Date;
            for (int i = 0; i < 42; i++)
            {
                int day = i - leading + 1;
                if (day < 1 || day > days)
                {
                    Border blank = new Border();
                    blank.Height = 28;
                    blank.Margin = new Thickness(1);
                    _dueDayGrid.Children.Add(blank);
                    continue;
                }
                DateTime date = new DateTime(_calendarMonth.Year, _calendarMonth.Month, day);
                bool selected = _selectedDate.HasValue && _selectedDate.Value.Date == date;
                bool isToday = date == today;
                Button cell = new Button();
                cell.Style = (Style)_root.Resources["CalendarDayStyle"];
                TextBlock label = UiKit.Text(day.ToString(), 9.5,
                    selected ? "#FF19141F" : (isToday ? "#FFC9BCFF" : "#FFDCD7E8"));
                if (selected || isToday) label.FontWeight = FontWeights.SemiBold;
                cell.Content = label;
                if (selected) cell.Background = UiKit.Brush("#E0BCA9FF");
                cell.Click += delegate
                {
                    _selectedDate = date;
                    _dueErrorText.Visibility = Visibility.Collapsed;
                    BuildCalendar();
                };
                AutomationProperties.SetName(cell, date.ToString("yyyy年M月d日") + " " + TaskService.WeekdayName(date.DayOfWeek));
                _dueDayGrid.Children.Add(cell);
            }
        }

        private void UpdateTimeFields()
        {
            if (_dueHourText != null) _dueHourText.Text = _selectedHour.ToString("00");
            if (_dueMinuteText != null) _dueMinuteText.Text = _selectedMinute.ToString("00");
        }

        private void HideTimeDropdown()
        {
            if (_dueTimeDropdownScroll != null) _dueTimeDropdownScroll.Visibility = Visibility.Collapsed;
            if (_dueTimeDropdownHint != null) _dueTimeDropdownHint.Visibility = Visibility.Collapsed;
        }

        private void ShowTimeDropdown(bool hours)
        {
            if (_dueTimeDropdownScroll.Visibility == Visibility.Visible && _timeDropdownIsHour == hours)
            {
                HideTimeDropdown();
                return;
            }
            _timeDropdownIsHour = hours;
            _dueTimeDropdownList.Children.Clear();
            int count = hours ? 24 : 60;
            int selected = hours ? _selectedHour : _selectedMinute;
            _dueTimeDropdownHint.Text = hours ? "选择小时" : "选择分钟";
            _dueTimeDropdownHint.Visibility = Visibility.Visible;
            for (int i = 0; i < count; i++)
            {
                int value = i;
                Button option = new Button();
                option.Style = (Style)_root.Resources["GhostButtonStyle"];
                option.Height = 23;
                option.HorizontalContentAlignment = HorizontalAlignment.Center;
                option.Margin = new Thickness(0, 1, 0, 1);
                TextBlock label = UiKit.Text(value.ToString("00"), 10.5,
                    value == selected ? "#FFF5F0FB" : "#FFBDB6C9");
                label.Foreground = ThemeManager.Brush(value == selected ? ThemeRole.AccentInk : ThemeRole.TextSecondary);
                if (value == selected) label.FontWeight = FontWeights.SemiBold;
                option.Content = label;
                if (value == selected) option.Background = ThemeManager.Brush(ThemeRole.Accent);
                option.Click += delegate
                {
                    if (hours) _selectedHour = value;
                    else _selectedMinute = value;
                    UpdateTimeFields();
                    HideTimeDropdown();
                };
                AutomationProperties.SetName(option, value.ToString("00") + (hours ? " 时" : " 分"));
                _dueTimeDropdownList.Children.Add(option);
            }
            _dueTimeDropdownScroll.Visibility = Visibility.Visible;
            double offset = Math.Max(0, (selected - 2) * 25);
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _dueTimeDropdownScroll.ScrollToVerticalOffset(offset);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ApplyDueEditor()
        {
            if (!_selectedDate.HasValue)
            {
                ShowDueError("请选择一个日期。");
                return;
            }
            DateTime selected = _selectedDate.Value.Date.AddHours(_selectedHour).AddMinutes(_selectedMinute);
            if (selected <= DateTime.Now.AddSeconds(-20))
            {
                ShowDueError("该时间已经过去，请选择稍后的时间。");
                return;
            }
            Action<DateTime?> apply = _dueApply;
            _duePopup.IsOpen = false;
            if (apply != null) apply(selected);
        }

        private void ShowDueError(string text)
        {
            _dueErrorText.Text = text;
            _dueErrorText.Visibility = Visibility.Visible;
        }
    }
}
