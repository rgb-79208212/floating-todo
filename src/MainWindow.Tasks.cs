using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FloatingTodo
{
    public partial class MainWindow
    {
        private readonly Dictionary<string, TaskRowView> _rowViews = new Dictionary<string, TaskRowView>();

        private class TaskRowView
        {
            public TodoTask Task;
            public Border Row;
            public TextBlock TitleBlock;
            public TextBox EditorBox;
            public Border EditorHost;
            public Button CheckButton;
            public Border CheckCircle;
            public Path CheckTick;
            public Ellipse CheckDot;
            public Button MoreButton;
            public bool InCompletedSection;
        }

        private void RebuildAll(bool preserveScroll)
        {
            if (_activeListPanel == null) return;
            if (_inlineEditing) return;
            double offset = _taskScroll != null ? _taskScroll.VerticalOffset : 0;

            _rowViews.Clear();
            _activeListPanel.Children.Clear();
            _completedListPanel.Children.Clear();

            List<TodoTask> activeAll = TaskService.ActiveSorted(_state);
            List<TodoTask> completedAll = TaskService.CompletedSorted(_state);
            List<TodoTask> active = FilterByTag(activeAll);
            List<TodoTask> completed = FilterByTag(completedAll);
            if (_activeTagFilter != null && FindTag(_activeTagFilter) == null) _activeTagFilter = null;
            if (_selectionMode)
            {
                HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < active.Count; i++) visible.Add(active[i].Id);
                for (int i = 0; i < completed.Count; i++) visible.Add(completed[i].Id);
                List<string> remove = new List<string>();
                foreach (string id in _selectedTaskIds) if (!visible.Contains(id)) remove.Add(id);
                for (int i = 0; i < remove.Count; i++) _selectedTaskIds.Remove(remove[i]);
            }

            for (int i = 0; i < active.Count; i++)
                _activeListPanel.Children.Add(BuildTaskRow(active[i], false));
            _emptyState.Visibility = active.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int completedCount = completed.Count;
            _completedHost.Visibility = completedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            _completedCountText.Text = "已完成 " + completedCount.ToString();
            bool expanded = _state.Settings.CompletedExpanded;
            _completedListPanel.Visibility = expanded && completedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            _clearCompletedButton.Visibility = expanded && completedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            _completedChevron.RenderTransform = new RotateTransform(expanded ? 180 : 0);
            _completedChevron.RenderTransformOrigin = new Point(0.5, 0.5);
            if (expanded)
            {
                for (int i = 0; i < completed.Count; i++)
                    _completedListPanel.Children.Add(BuildTaskRow(completed[i], true));
            }
            if (!expanded && _selectionMode)
            {
                List<string> remove = new List<string>();
                foreach (string id in _selectedTaskIds)
                {
                    TodoTask t = FindTask(id);
                    if (t != null && t.Done) remove.Add(id);
                }
                for (int i = 0; i < remove.Count; i++) _selectedTaskIds.Remove(remove[i]);
            }

            UpdateHeader();
            _activeCountText.Text = active.Count.ToString();
            BuildTagFilterBar();
            UpdateSelectionUi();
            _lastRenderedMinute = DateTime.Now.ToString("yyyyMMddHHmm");

            if (preserveScroll && offset > 0)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (_taskScroll != null)
                        _taskScroll.ScrollToVerticalOffset(Math.Min(offset, _taskScroll.ScrollableHeight));
                }), DispatcherPriority.Loaded);
            }
        }

        private List<TodoTask> FilterByTag(List<TodoTask> source)
        {
            if (string.IsNullOrEmpty(_activeTagFilter)) return source;
            List<TodoTask> result = new List<TodoTask>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].TagIds != null && source[i].TagIds.Contains(_activeTagFilter)) result.Add(source[i]);
            }
            return result;
        }

        private TagDefinition FindTag(string id)
        {
            if (string.IsNullOrEmpty(id) || _state.Tags == null) return null;
            for (int i = 0; i < _state.Tags.Count; i++)
                if (string.Equals(_state.Tags[i].Id, id, StringComparison.Ordinal)) return _state.Tags[i];
            return null;
        }

        private void BuildTagFilterBar()
        {
            if (_tagFilterScroll == null || _tagFilterPanel == null) return;
            _tagFilterPanel.Children.Clear();
            if (_state.Tags == null || _state.Tags.Count == 0)
            {
                _tagFilterScroll.Visibility = Visibility.Collapsed;
                _activeTagFilter = null;
                return;
            }
            _tagFilterScroll.Visibility = Visibility.Visible;
            Button all = CreateFilterChip("全部", null, string.IsNullOrEmpty(_activeTagFilter));
            _tagFilterPanel.Children.Add(all);
            for (int i = 0; i < _state.Tags.Count; i++)
            {
                TagDefinition tag = _state.Tags[i];
                Button chip = CreateFilterChip(tag.Name, tag, string.Equals(_activeTagFilter, tag.Id, StringComparison.Ordinal));
                _tagFilterPanel.Children.Add(chip);
            }
        }

        private Button CreateFilterChip(string text, TagDefinition tag, bool selected)
        {
            Button button = new Button();
            button.Style = (Style)_root.Resources["ChipButtonStyle"];
            button.Margin = new Thickness(0, 1, 4, 1);
            button.Padding = new Thickness(7, 2, 7, 2);
            StackPanel content = new StackPanel();
            content.Orientation = Orientation.Horizontal;
            if (tag != null)
            {
                Ellipse dot = new Ellipse();
                dot.Width = 5;
                dot.Height = 5;
                dot.Fill = UiKit.BrushExact(tag.Color);
                dot.Margin = new Thickness(0, 0, 4, 0);
                dot.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(dot);
            }
            content.Children.Add(UiKit.Text(text, 8.6, selected ? "#FFF7F4FB" : "#FFB4ADC0"));
            button.Content = content;
            if (selected)
            {
                button.Background = UiKit.Brush("#2CBCA9FF");
                button.BorderBrush = UiKit.Brush("#66BCA9FF");
            }
            string tagId = tag == null ? null : tag.Id;
            button.Click += delegate
            {
                _activeTagFilter = _activeTagFilter == tagId ? null : tagId;
                RebuildAll(true);
            };
            AutomationProperties.SetName(button, "标签筛选：" + text);
            return button;
        }

        private Border BuildTaskRow(TodoTask task, bool completedSection)
        {
            bool urgent = !task.Done && TaskService.IsUrgent(task, DateTime.Now, _state.Settings.LeadMinutes);
            bool overdue = !task.Done && TaskService.IsOverdue(task, DateTime.Now);

            Border row = new Border();
            row.CornerRadius = new CornerRadius(13);
            row.Padding = new Thickness(8, 7, 4, 7);
            row.Margin = new Thickness(0, 2, 0, 2);
            row.Background = completedSection ? Brushes.Transparent : ThemeManager.Brush(ThemeRole.TaskSurface);
            row.Tag = task;
            if (!completedSection)
            {
                row.Effect = ThemeManager.Palette.TaskShadow;
            }
            if (_selectionMode && _selectedTaskIds.Contains(task.Id)) row.Background = SelectedRowBackground();

            TaskRowView view = new TaskRowView();
            view.Task = task;
            view.Row = row;
            view.InCompletedSection = completedSection;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });

            Button check = new Button();
            check.Style = (Style)_root.Resources["CheckButtonStyle"];
            check.Width = 27;
            check.Height = 27;
            check.VerticalAlignment = VerticalAlignment.Top;
            check.Margin = new Thickness(0, 1, 0, 0);
            check.ToolTip = _selectionMode ? (_selectedTaskIds.Contains(task.Id) ? "取消选择" : "选择") : (task.Done ? "恢复为待办" : "标记为已完成");
            AutomationProperties.SetName(check, _selectionMode
                ? (_selectedTaskIds.Contains(task.Id) ? "取消选择：" : "选择：") + task.Title
                : (task.Done ? "恢复为待办：" : "标记为已完成：") + task.Title);

            Grid checkVisual = new Grid();
            checkVisual.Width = 21;
            checkVisual.Height = 21;
            Border circle = new Border();
            circle.CornerRadius = new CornerRadius(10.5);
            circle.BorderThickness = new Thickness(1.15);
            Path tick = new Path();
            tick.Data = Geometry.Parse("M3.3,8.4 L6.6,11.6 L12.7,4.9");
            tick.Stroke = UiKit.Brush("#FF19141F");
            tick.StrokeThickness = 1.6;
            tick.StrokeStartLineCap = PenLineCap.Round;
            tick.StrokeEndLineCap = PenLineCap.Round;
            tick.Stretch = Stretch.Uniform;
            tick.Margin = new Thickness(5);
            Ellipse dot = new Ellipse();
            dot.Width = 5;
            dot.Height = 5;
            dot.Fill = UiKit.Brush("#FFFF8792");
            dot.Visibility = Visibility.Collapsed;

            bool selected = _selectionMode && _selectedTaskIds.Contains(task.Id);
            if (_selectionMode)
            {
                if (selected)
                {
                    circle.Background = UiKit.Brush("#FFBCA9FF");
                    circle.BorderBrush = UiKit.Brush("#FFBCA9FF");
                    tick.Stroke = UiKit.Brush("#FF19141F");
                    tick.Visibility = Visibility.Visible;
                }
                else
                {
                    circle.Background = Brushes.Transparent;
                    circle.BorderBrush = UiKit.Brush("#56FFFFFF");
                    tick.Visibility = Visibility.Collapsed;
                }
            }
            else if (task.Done)
            {
                circle.Background = UiKit.Brush("#FFBCA9FF");
                circle.BorderBrush = UiKit.Brush("#FFBCA9FF");
                tick.Stroke = UiKit.Brush("#FF19141F");
                tick.Visibility = Visibility.Visible;
            }
            else
            {
                circle.Background = Brushes.Transparent;
                circle.BorderBrush = UiKit.Brush("#4DFFFFFF");
                tick.Visibility = Visibility.Collapsed;
                dot.Visibility = Visibility.Collapsed;
            }
            checkVisual.Children.Add(circle);
            checkVisual.Children.Add(tick);
            checkVisual.Children.Add(dot);
            check.Content = checkVisual;
            check.Click += delegate(object sender, RoutedEventArgs e)
            {
                e.Handled = true;
                if (_selectionMode) ToggleTaskSelection(task);
                else ToggleDone(task);
            };
            Grid.SetColumn(check, 0);
            grid.Children.Add(check);

            StackPanel content = new StackPanel();
            content.VerticalAlignment = VerticalAlignment.Center;
            content.Margin = new Thickness(1, 0, 5, 0);
            Grid.SetColumn(content, 1);

            Grid titleHost = new Grid();
            TextBlock title = new TextBlock();
            title.Text = task.Title;
            title.FontSize = 12.6;
            title.Foreground = UiKit.Brush(task.Done ? "#FF918A9D" : "#FFF7F4FB");
            title.TextWrapping = TextWrapping.Wrap;
            title.MaxHeight = 35;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.ToolTip = task.Title;
            if (task.Done) title.TextDecorations = TextDecorations.Strikethrough;
            titleHost.Children.Add(title);

            Border editorHost = new Border();
            editorHost.CornerRadius = new CornerRadius(6);
            editorHost.Background = UiKit.Brush("#20FFFFFF");
            editorHost.BorderBrush = UiKit.Brush("#66BCA9FF");
            editorHost.BorderThickness = new Thickness(1);
            editorHost.Padding = new Thickness(6, 0, 6, 0);
            editorHost.Height = 27;
            editorHost.Visibility = Visibility.Collapsed;
            TextBox editor = new TextBox();
            editor.FontSize = 11.8;
            editor.Foreground = UiKit.Brush("#FFF8F5FF");
            editor.Background = Brushes.Transparent;
            editor.BorderThickness = new Thickness(0);
            editor.Padding = new Thickness(0);
            editor.VerticalContentAlignment = VerticalAlignment.Center;
            editor.MaxLength = 160;
            editor.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { e.Handled = true; CommitInlineEdit(true); }
                else if (e.Key == Key.Escape) { e.Handled = true; CancelInlineEdit(); }
            };
            editor.LostKeyboardFocus += delegate
            {
                if (_inlineEditing && !_finishingInline && _editingTask == task) CommitInlineEdit(false);
            };
            editorHost.Child = editor;
            titleHost.Children.Add(editorHost);
            content.Children.Add(titleHost);

            Border timePill = BuildTimePill(task, completedSection, urgent, overdue);
            if (timePill != null) content.Children.Add(timePill);
            if (task.TagIds != null && task.TagIds.Count > 0)
            {
                WrapPanel tags = new WrapPanel();
                tags.Margin = new Thickness(0, 4, 0, 0);
                int shown = 0;
                for (int ti = 0; ti < task.TagIds.Count && shown < 3; ti++)
                {
                    TagDefinition tag = FindTag(task.TagIds[ti]);
                    if (tag == null) continue;
                    Border pill = new Border();
                    pill.CornerRadius = new CornerRadius(5);
                    pill.Padding = new Thickness(5, 1, 6, 1);
                    pill.Margin = new Thickness(0, 0, 4, 0);
                    pill.Background = TintBrush(tag.Color, 34);
                    StackPanel sp = new StackPanel();
                    sp.Orientation = Orientation.Horizontal;
                    Ellipse tagDot = new Ellipse();
                    tagDot.Width = 4;
                    tagDot.Height = 4;
                    tagDot.Fill = UiKit.BrushExact(tag.Color);
                    tagDot.Margin = new Thickness(0, 0, 4, 0);
                    tagDot.VerticalAlignment = VerticalAlignment.Center;
                    sp.Children.Add(tagDot);
                    sp.Children.Add(UiKit.TextExact(tag.Name, 8.1, tag.Color));
                    pill.Child = sp;
                    tags.Children.Add(pill);
                    shown++;
                }
                if (task.TagIds.Count > shown) tags.Children.Add(UiKit.Text("+" + (task.TagIds.Count - shown).ToString(), 8.1, "#FF8F889C"));
                content.Children.Add(tags);
            }
            if (!string.IsNullOrEmpty(task.Note))
            {
                TextBlock note = UiKit.Text("备注：" + task.Note.Replace("\r", " ").Replace("\n", " "), 9, "#FF8E879B");
                note.TextTrimming = TextTrimming.CharacterEllipsis;
                note.TextWrapping = TextWrapping.NoWrap;
                note.Margin = new Thickness(0, 4, 0, 0);
                note.ToolTip = task.Note;
                content.Children.Add(note);
            }

            grid.Children.Add(content);

            Button more = new Button();
            more.Style = (Style)_root.Resources["IconButtonStyle"];
            more.Width = 23;
            more.Height = 23;
            more.Background = Brushes.Transparent;
            more.BorderThickness = new Thickness(0);
            more.Opacity = task.Done ? 0.4 : 0.72;
            more.Content = UiKit.MoreIcon(13, task.Done ? "#B7B1C3" : "#E5E0EE");
            more.ToolTip = "更多操作";
            AutomationProperties.SetName(more, "更多操作 · " + Shorten(task.Title, 18));
            more.Visibility = _selectionMode ? Visibility.Collapsed : Visibility.Visible;
            more.VerticalAlignment = VerticalAlignment.Center;
            more.HorizontalAlignment = HorizontalAlignment.Right;
            more.Click += delegate(object sender, RoutedEventArgs e)
            {
                e.Handled = true;
                OpenTaskMenu(task, more);
            };
            Grid.SetColumn(more, 2);
            grid.Children.Add(more);

            row.Child = grid;
            row.MouseEnter += delegate
            {
                if (_selectionMode)
                {
                    if (!_selectedTaskIds.Contains(task.Id)) row.Background = ThemeManager.Brush(ThemeRole.TaskHover);
                }
                else if (!task.Done && !completedSection)
                {
                    row.Background = ThemeManager.Brush(ThemeRole.TaskHover);
                }
                more.Opacity = 1;
            };
            row.MouseLeave += delegate
            {
                if (_selectionMode) row.Background = _selectedTaskIds.Contains(task.Id) ? SelectedRowBackground() : (completedSection ? Brushes.Transparent : ThemeManager.Brush(ThemeRole.TaskSurface));
                else if (!task.Done && !completedSection) row.Background = ThemeManager.Brush(ThemeRole.TaskSurface);
                more.Opacity = task.Done ? 0.4 : 0.72;
            };
            if (_selectionMode)
            {
                row.Cursor = Cursors.Hand;
                row.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
                {
                    if (_suppressTaskClick) return;
                    if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null) return;
                    if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null) return;
                    ToggleTaskSelection(task);
                    e.Handled = true;
                };
            }
            else
            {
                row.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
                {
                    if (_suppressTaskClick) return;
                    if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null) return;
                    if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null) return;
                    OpenTaskEditor(task);
                };
                row.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    OpenTaskMenu(task, row);
                };
            }

            view.TitleBlock = title;
            view.EditorBox = editor;
            view.EditorHost = editorHost;
            view.CheckButton = check;
            view.CheckCircle = circle;
            view.CheckTick = tick;
            view.CheckDot = dot;
            view.MoreButton = more;
            _rowViews[task.Id] = view;
            return row;
        }

        private Border BuildTimePill(TodoTask task, bool completedSection, bool urgent, bool overdue)
        {
            ThemePalette palette = ThemeManager.Palette;
            Border hit = new Border();
            hit.Margin = new Thickness(0, 5, 0, 0);
            hit.HorizontalAlignment = HorizontalAlignment.Left;
            hit.CornerRadius = new CornerRadius(8);
            hit.Padding = new Thickness(6, 2, 7, 2);
            hit.BorderThickness = new Thickness(0);
            hit.Cursor = Cursors.Hand;
            hit.ToolTip = "更改截止时间";
            if (completedSection)
                hit.Background = Brushes.Transparent;
            else if (urgent)
                hit.Background = ThemeManager.Brush(ThemeRole.UrgentMarker);
            else
                hit.Background = ThemeManager.Brush(ThemeRole.TaskSurfaceAlt);

            StackPanel meta = new StackPanel();
            meta.Orientation = Orientation.Horizontal;
            meta.VerticalAlignment = VerticalAlignment.Center;
            hit.Child = meta;

            if (task.Done)
            {
                FrameworkElement checkIcon = UiKit.CheckIcon(9, "#FF8E889B", 1.2);
                checkIcon.Margin = new Thickness(0, 0, 4, 0);
                meta.Children.Add(checkIcon);
                TextBlock done = UiKit.Text("已完成", 8.8, "#FF8E889B");
                meta.Children.Add(done);
                if (task.CompletedAt.HasValue)
                {
                    meta.Children.Add(UiKit.Text(" · " + task.CompletedAt.Value.ToString("M/d HH:mm"), 8.2, "#FF7A7386"));
                }
                return hit;
            }

            if (!task.DueAt.HasValue)
            {
                FrameworkElement plus = UiKit.Icon("M8,3.2 L8,12.8 M3.2,8 L12.8,8", 8.5, "#FF827B8E", 1.2);
                plus.Margin = new Thickness(0, 0, 4, 0);
                meta.Children.Add(plus);
                meta.Children.Add(UiKit.Text("添加时间", 9, "#FF8B8498"));
                hit.Cursor = Cursors.Hand;
                hit.Opacity = 0.7;
                hit.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
                {
                    if (_selectionMode) return;
                    e.Handled = true;
                    OpenDueEditorForTask(task, hit);
                };
                return hit;
            }

            DuePresentation due = TaskService.DescribeDue(task, DateTime.Now, _state.Settings.LeadMinutes);
            if (due.Urgent)
            {
                Ellipse dot = new Ellipse();
                dot.Width = 4;
                dot.Height = 4;
                dot.Fill = ThemeManager.Brush(ThemeRole.UrgentMarkerInk);
                dot.Margin = new Thickness(0, 0, 5, 0);
                dot.VerticalAlignment = VerticalAlignment.Center;
                meta.Children.Add(dot);
            }
            else
            {
                FrameworkElement clock = UiKit.ClockIcon(9.2, "#FFB2AABF", 1.1);
                clock.Margin = new Thickness(0, 0, 4, 0);
                meta.Children.Add(clock);
            }
            TextBlock primary = UiKit.Text(due.Primary, 10.3, due.Urgent ? "#FFFFA0A8" : "#FFF0EBF7");
            primary.Foreground = ThemeManager.Brush(due.Urgent ? ThemeRole.UrgentMarkerInk : ThemeRole.TimeText);
            primary.FontWeight = FontWeights.SemiBold;
            meta.Children.Add(primary);
            if (!string.IsNullOrEmpty(due.Secondary))
            {
                TextBlock secondary = UiKit.Text(" · " + due.Secondary, 8.7,
                    due.Overdue ? "#FFFF8E99" : "#FFFFA0A8");
                secondary.Foreground = ThemeManager.Brush(due.Urgent ? ThemeRole.UrgentMarkerSub : ThemeRole.TimeSub);
                secondary.FontWeight = FontWeights.SemiBold;
                meta.Children.Add(secondary);
            }

            if (!_selectionMode)
            {
                hit.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    OpenDueEditorForTask(task, hit);
                };
            }
            return hit;
        }

        private LinearGradientBrush UrgentRowBackground()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 0);
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(27, 255, 98, 115), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(11, 255, 98, 115), 0.6));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 98, 115), 1));
            brush.Freeze();
            return brush;
        }

        private LinearGradientBrush UrgentRowHoverBackground()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 0);
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(40, 255, 98, 115), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(16, 255, 98, 115), 0.6));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 98, 115), 1));
            brush.Freeze();
            return brush;
        }

        private Brush SelectedRowBackground()
        {
            return ThemeManager.Brush(ThemeRole.Selection);
        }

        private Brush TintBrush(string hex, int alpha)
        {
            try
            {
                Color color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(Color.FromArgb((byte)alpha, color.R, color.G, color.B));
            }
            catch { return UiKit.Brush("#16FFFFFF"); }
        }

        private void ToggleDone(TodoTask task)
        {
            if (task == null) return;
            TaskService.SetDone(task, !task.Done, DateTime.Now);
            _store.SaveAsync();
            RebuildAll(true);
            UpdateTrayState();
        }

        private void ToggleCompletedSection()
        {
            _state.Settings.CompletedExpanded = !_state.Settings.CompletedExpanded;
            _store.SaveAsync();
            RebuildAll(true);
        }

        private void ScrollNewTaskIntoView()
        {
            if (string.IsNullOrEmpty(_newRowId)) return;
            TaskRowView view;
            if (_rowViews.TryGetValue(_newRowId, out view))
            {
                view.Row.BringIntoView();
            }
            _newRowId = null;
        }

        private void BeginInlineEdit(TodoTask task, TaskRowView view)
        {
            if (_selectionMode || task == null || view == null) return;
            if (_inlineEditing) CommitInlineEdit(false);
            CloseTransientPopups();
            _inlineEditing = true;
            _editingTask = task;
            _editingView = view;
            view.TitleBlock.Visibility = Visibility.Collapsed;
            view.EditorHost.Visibility = Visibility.Visible;
            view.EditorBox.Text = task.Title;
            view.MoreButton.Opacity = 0.2;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                view.EditorBox.Focus();
                Keyboard.Focus(view.EditorBox);
                view.EditorBox.SelectAll();
            }), DispatcherPriority.Input);
        }

        private void CommitInlineEdit(bool refocusInput)
        {
            if (!_inlineEditing || _editingTask == null) return;
            _finishingInline = true;
            string next = TaskService.NormalizeTitle(_editingView.EditorBox.Text);
            TodoTask task = _editingTask;
            bool changed = next.Length > 0 && !string.Equals(next, task.Title, StringComparison.Ordinal);
            _inlineEditing = false;
            _editingTask = null;
            _editingView = null;
            if (changed)
            {
                task.Title = next;
                _store.SaveAsync();
            }
            _finishingInline = false;
            RebuildAll(true);
            UpdateTrayState();
            if (refocusInput) FocusInput();
        }

        private void CancelInlineEdit()
        {
            if (!_inlineEditing) return;
            _finishingInline = true;
            _inlineEditing = false;
            _editingTask = null;
            _editingView = null;
            _finishingInline = false;
            RebuildAll(true);
        }

        private TodoTask FindTask(string id)
        {
            for (int i = 0; i < _state.Tasks.Count; i++)
                if (_state.Tasks[i] != null && string.Equals(_state.Tasks[i].Id, id, StringComparison.Ordinal)) return _state.Tasks[i];
            return null;
        }

        private List<TodoTask> GetSelectedTasks()
        {
            List<TodoTask> result = new List<TodoTask>();
            for (int i = 0; i < _state.Tasks.Count; i++)
                if (_state.Tasks[i] != null && _selectedTaskIds.Contains(_state.Tasks[i].Id)) result.Add(_state.Tasks[i]);
            return result;
        }

        private List<TodoTask> GetSelectableTasks()
        {
            List<TodoTask> result = FilterByTag(TaskService.ActiveSorted(_state));
            if (_state.Settings.CompletedExpanded)
            {
                List<TodoTask> done = FilterByTag(TaskService.CompletedSorted(_state));
                for (int i = 0; i < done.Count; i++) result.Add(done[i]);
            }
            return result;
        }

        private void ToggleSelectionMode()
        {
            if (_selectionMode)
            {
                ExitSelectionMode();
                return;
            }
            if (_state.Tasks.Count == 0) return;
            _selectionMode = true;
            _selectedTaskIds.Clear();
            _normalFooter.Visibility = Visibility.Collapsed;
            _batchBar.Visibility = Visibility.Visible;
            _selectModeText.Text = "取消";
            RebuildAll(true);
        }

        private void ExitSelectionMode()
        {
            _selectionMode = false;
            _selectedTaskIds.Clear();
            _normalFooter.Visibility = Visibility.Visible;
            _batchBar.Visibility = Visibility.Collapsed;
            _selectModeText.Text = "选择";
            RebuildAll(true);
        }

        private void ToggleTaskSelection(TodoTask task)
        {
            if (task == null || !_selectionMode) return;
            if (!_selectedTaskIds.Add(task.Id)) _selectedTaskIds.Remove(task.Id);
            TaskRowView view;
            if (_rowViews.TryGetValue(task.Id, out view)) UpdateRowSelectionVisual(view);
            UpdateSelectionUi();
        }

        private void UpdateRowSelectionVisual(TaskRowView view)
        {
            if (view == null || !_selectionMode) return;
            bool selected = _selectedTaskIds.Contains(view.Task.Id);
            view.Row.Background = selected ? SelectedRowBackground() : ThemeManager.Brush(ThemeRole.TaskSurface);
            if (selected)
            {
                view.CheckCircle.Background = UiKit.Brush("#FFBCA9FF");
                view.CheckCircle.BorderBrush = UiKit.Brush("#FFBCA9FF");
                view.CheckTick.Visibility = Visibility.Visible;
            }
            else
            {
                view.CheckCircle.Background = Brushes.Transparent;
                view.CheckCircle.BorderBrush = UiKit.Brush("#56FFFFFF");
                view.CheckTick.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSelectionUi()
        {
            if (_selectionCountText == null) return;
            int count = _selectedTaskIds.Count;
            int total = _state.Tasks.Count;
            _selectionCountText.Text = "已选 " + count.ToString() + " 项";
            _selectModeButton.Visibility = total > 0 ? Visibility.Visible : Visibility.Collapsed;
            _selectAllText.Text = AllSelectableSelected() ? "清空" : "全选";
            List<TodoTask> selected = GetSelectedTasks();
            bool anyActive = false;
            bool allDone = selected.Count > 0;
            for (int i = 0; i < selected.Count; i++)
            {
                if (!selected[i].Done) { anyActive = true; allDone = false; }
            }
            if (selected.Count == 0) allDone = false;
            TextBlock doneLabel = _batchDoneButton.Content as TextBlock;
            if (doneLabel != null) doneLabel.Text = allDone ? "恢复" : "完成";
            bool hasSelection = count > 0;
            SetActionEnabled(_batchDoneButton, hasSelection);
            SetActionEnabled(_batchDueButton, anyActive);
            SetActionEnabled(_batchDeleteButton, hasSelection);
            _clearCompletedButton.IsEnabled = !_selectionMode;
        }

        private static void SetActionEnabled(Button button, bool enabled)
        {
            if (button == null) return;
            button.IsEnabled = enabled;
            button.Opacity = enabled ? 1 : 0.38;
        }

        private bool AllSelectableSelected()
        {
            List<TodoTask> tasks = GetSelectableTasks();
            if (tasks.Count == 0) return false;
            for (int i = 0; i < tasks.Count; i++)
                if (!_selectedTaskIds.Contains(tasks[i].Id)) return false;
            return true;
        }

        private void ToggleSelectAll()
        {
            List<TodoTask> tasks = GetSelectableTasks();
            if (AllSelectableSelected())
            {
                _selectedTaskIds.Clear();
            }
            else
            {
                for (int i = 0; i < tasks.Count; i++) _selectedTaskIds.Add(tasks[i].Id);
            }
            for (int i = 0; i < tasks.Count; i++)
            {
                TaskRowView view;
                if (_rowViews.TryGetValue(tasks[i].Id, out view)) UpdateRowSelectionVisual(view);
            }
            UpdateSelectionUi();
        }

        private void ConfirmBatchDone()
        {
            List<TodoTask> selected = GetSelectedTasks();
            if (selected.Count == 0) return;
            bool anyActive = false;
            for (int i = 0; i < selected.Count; i++) if (!selected[i].Done) { anyActive = true; break; }
            bool makeDone = anyActive;
            string verb = makeDone ? "完成" : "恢复";
            string title = "确定" + verb + "选中的 " + selected.Count.ToString() + " 项？";
            string body = makeDone ? "选中的待办会移到已完成列表。" : "选中的待办会恢复到待办列表。";
            ShowConfirmModal(title, body, verb, delegate
            {
                TaskService.MarkAll(selected, makeDone, DateTime.Now);
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

        private void ConfirmBatchDelete()
        {
            List<TodoTask> selected = GetSelectedTasks();
            if (selected.Count == 0) return;
            ShowConfirmModal("删除选中的 " + selected.Count.ToString() + " 项？",
                "这些待办将被永久删除，此操作无法撤销。", "删除", delegate
                {
                    List<string> ids = new List<string>();
                    for (int i = 0; i < selected.Count; i++) ids.Add(selected[i].Id);
                    TaskService.Remove(_state, ids);
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

        private void ConfirmDeleteTask(TodoTask task)
        {
            if (task == null) return;
            ShowConfirmModal("删除这条待办？", "删除后无法恢复。", "删除", delegate
            {
                List<string> ids = new List<string>();
                ids.Add(task.Id);
                TaskService.Remove(_state, ids);
                _store.SaveAsync();
                RebuildAll(true);
                UpdateTrayState();
            });
        }

        private void ConfirmClearCompleted()
        {
            int count = 0;
            for (int i = 0; i < _state.Tasks.Count; i++) if (_state.Tasks[i].Done) count++;
            if (count == 0) return;
            ShowConfirmModal("清除 " + count.ToString() + " 项已完成待办？",
                "这些待办将被永久删除，此操作无法撤销。", "清除", delegate
                {
                    TaskService.ClearCompleted(_state);
                    _store.SaveAsync();
                    RebuildAll(true);
                    UpdateTrayState();
                });
        }

        private void OnListMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null) return;
            if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null) return;
            _dragPending = true;
            _dragScrolling = false;
            _dragStartPoint = e.GetPosition(_taskScroll);
            _dragStartOffset = _taskScroll.VerticalOffset;
        }

        private void OnListMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragPending || e.LeftButton != MouseButtonState.Pressed) return;
            Point current = e.GetPosition(_taskScroll);
            double delta = current.Y - _dragStartPoint.Y;
            if (!_dragScrolling && Math.Abs(delta) > 4)
            {
                _dragScrolling = true;
                _taskScroll.CaptureMouse();
                _taskScroll.Cursor = Cursors.ScrollNS;
                _suppressTaskClick = true;
            }
            if (_dragScrolling)
            {
                _taskScroll.ScrollToVerticalOffset(_dragStartOffset - delta);
                e.Handled = true;
            }
        }

        private void OnListMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragScrolling)
            {
                e.Handled = true;
            }
            EndListDrag();
        }

        private void EndListDrag()
        {
            if (!_dragPending && !_dragScrolling) return;
            _dragPending = false;
            if (_dragScrolling)
            {
                _dragScrolling = false;
                _taskScroll.ReleaseMouseCapture();
                _taskScroll.Cursor = Cursors.Arrow;
                Dispatcher.BeginInvoke(new Action(delegate { _suppressTaskClick = false; }), DispatcherPriority.Input);
            }
        }
    }
}
