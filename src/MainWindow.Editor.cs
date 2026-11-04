using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FloatingTodo
{
    public partial class MainWindow
    {
        private static readonly string[] TagPalette = new string[]
        {
            "#B79BFF", "#65D6BB", "#F0A55F", "#6FB7FF", "#E58BBF", "#D6C764", "#FF8F95"
        };

        private void OpenTaskEditor(TodoTask task)
        {
            if (task == null || _selectionMode) return;
            CloseTransientPopups();
            _editingTaskDetail = task;
            _editorSelectedTags = new List<string>();
            if (task.TagIds != null)
                for (int i = 0; i < task.TagIds.Count; i++) _editorSelectedTags.Add(task.TagIds[i]);
            _editorPendingDue = task.DueAt;
            _editorDueChanged = false;
            _editorTitleBox.Text = task.Title ?? "";
            _editorNoteBox.Text = task.Note ?? "";
            _editorNewTagBox.Text = "";
            _editorErrorText.Visibility = Visibility.Collapsed;
            UpdateEditorDueText();
            RefreshEditorTags();
            _editorOverlay.Visibility = Visibility.Visible;
            _editorOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            _editorCard.BeginAnimation(UIElement.OpacityProperty, null);
            _editorOverlay.Opacity = 0;
            _editorCard.Opacity = 0;
            DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            _editorOverlay.BeginAnimation(UIElement.OpacityProperty, fade);
            _editorCard.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _editorTitleBox.Focus();
                _editorTitleBox.CaretIndex = _editorTitleBox.Text.Length;
            }), DispatcherPriority.Input);
        }

        private void CloseTaskEditor()
        {
            if (_editorOverlay == null || _editorOverlay.Visibility != Visibility.Visible) return;
            _editingTaskDetail = null;
            _editorSelectedTags = null;
            _editorDueChanged = false;
            DoubleAnimation fade = new DoubleAnimation(_editorOverlay.Opacity, 0, TimeSpan.FromMilliseconds(100));
            fade.Completed += delegate
            {
                _editorOverlay.Visibility = Visibility.Collapsed;
                _editorOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            };
            _editorOverlay.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void SaveTaskEditor()
        {
            if (_editingTaskDetail == null) return;
            string title = TaskService.NormalizeTitle(_editorTitleBox.Text);
            if (title.Length == 0)
            {
                _editorErrorText.Text = "标题不能为空。";
                _editorErrorText.Visibility = Visibility.Visible;
                _editorTitleBox.Focus();
                return;
            }
            TodoTask task = _editingTaskDetail;
            task.Title = title;
            task.Note = _editorNoteBox.Text == null ? "" : _editorNoteBox.Text.Trim();
            if (task.Note.Length > 3000) task.Note = task.Note.Substring(0, 3000);
            List<string> tags = new List<string>();
            if (_editorSelectedTags != null)
                for (int i = 0; i < _editorSelectedTags.Count; i++)
                    if (FindTag(_editorSelectedTags[i]) != null) tags.Add(_editorSelectedTags[i]);
            task.TagIds = tags;
            if (_editorDueChanged) task.DueAt = TaskService.NormalizeDue(_editorPendingDue);
            _store.SaveAsync();
            CloseTaskEditor();
            RebuildAll(true);
            UpdateTrayState();
        }

        private void RefreshEditorTags()
        {
            _editorTagPanel.Children.Clear();
            if (_state.Tags.Count == 0)
            {
                TextBlock empty = UiKit.Text("还没有标签，输入名称后添加", 8.4, "#FF7F798B");
                empty.Margin = new Thickness(1, 4, 0, 4);
                _editorTagPanel.Children.Add(empty);
                return;
            }
            for (int i = 0; i < _state.Tags.Count; i++)
            {
                TagDefinition tag = _state.Tags[i];
                bool selected = _editorSelectedTags != null && _editorSelectedTags.Contains(tag.Id);
                Button chip = new Button();
                chip.Style = (Style)_root.Resources["ChipButtonStyle"];
                chip.Padding = new Thickness(7, 3, 7, 3);
                chip.Margin = new Thickness(0, 0, 4, 4);
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                Ellipse dot = new Ellipse();
                dot.Width = 5;
                dot.Height = 5;
                dot.Fill = UiKit.BrushExact(tag.Color);
                dot.Margin = new Thickness(0, 0, 4, 0);
                dot.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(dot);
                content.Children.Add(selected
                    ? UiKit.Text(tag.Name, 8.6, "#FFF9F6FF")
                    : UiKit.TextExact(tag.Name, 8.6, tag.Color));
                chip.Content = content;
                if (selected)
                {
                    chip.Background = TintBrush(tag.Color, 54);
                    chip.BorderBrush = TintBrush(tag.Color, 120);
                }
                string id = tag.Id;
                chip.Click += delegate { ToggleEditorTag(id); };
                AutomationProperties.SetName(chip, (selected ? "取消标签：" : "添加标签：") + tag.Name);
                _editorTagPanel.Children.Add(chip);
            }
        }

        private void ToggleEditorTag(string tagId)
        {
            if (_editorSelectedTags == null) return;
            if (!_editorSelectedTags.Remove(tagId)) _editorSelectedTags.Add(tagId);
            RefreshEditorTags();
        }

        private void AddEditorTag()
        {
            string name = TaskService.NormalizeTitle(_editorNewTagBox.Text);
            if (name.Length == 0) return;
            if (name.Length > 20) name = name.Substring(0, 20);
            TagDefinition existing = null;
            for (int i = 0; i < _state.Tags.Count; i++)
                if (string.Equals(_state.Tags[i].Name, name, StringComparison.OrdinalIgnoreCase)) { existing = _state.Tags[i]; break; }
            if (existing == null)
            {
                existing = new TagDefinition(Guid.NewGuid().ToString("N"), name, TagPalette[_state.Tags.Count % TagPalette.Length]);
                _state.Tags.Add(existing);
                _store.SaveAsync();
            }
            if (_editorSelectedTags == null) _editorSelectedTags = new List<string>();
            if (!_editorSelectedTags.Contains(existing.Id)) _editorSelectedTags.Add(existing.Id);
            _editorNewTagBox.Text = "";
            RefreshEditorTags();
        }

        private void UpdateEditorDueText()
        {
            if (_editorDueButton == null) return;
            string text;
            if (_editorPendingDue.HasValue) text = "截止：" + TaskService.FormatDue(_editorPendingDue.Value, DateTime.Now);
            else text = "设置截止时间";
            _editorDueButton.Content = new TextBlock
            {
                Text = text,
                FontSize = 9.2,
                Foreground = UiKit.Brush("#E7E1F1")
            };
        }

        private void OpenDueEditorForEditor()
        {
            if (_editingTaskDetail == null) return;
            DateTime initial = _editorPendingDue.HasValue ? _editorPendingDue.Value : DefaultDueTime();
            OpenDueEditor(_editorDueButton, initial, _editorPendingDue.HasValue, delegate(DateTime? value)
            {
                _editorPendingDue = value;
                _editorDueChanged = true;
                UpdateEditorDueText();
            });
        }

        private void OnResizeGripDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (_options.DevMode) _store.Log("[debug] resize grip down", null);
            _resizing = true;
            _resizeStartScreen = PointToScreen(e.GetPosition(this));
            _resizeStartWidth = Width;
            _resizeStartHeight = Height;
            _resizeGrip.CaptureMouse();
            e.Handled = true;
        }

        private void OnResizeGripMove(object sender, MouseEventArgs e)
        {
            if (!_resizing || e.LeftButton != MouseButtonState.Pressed) return;
            Point now = PointToScreen(e.GetPosition(this));
            double dx = now.X - _resizeStartScreen.X;
            double dy = now.Y - _resizeStartScreen.Y;
            Rect work = SystemParameters.WorkArea;
            double maxWidth = Math.Max(330, work.Right - Left - 6);
            double maxHeight = Math.Max(460, work.Bottom - Top - 6);
            Width = Math.Min(maxWidth, Math.Max(330, _resizeStartWidth + dx));
            Height = Math.Min(maxHeight, Math.Max(460, _resizeStartHeight + dy));
            e.Handled = true;
        }

        private void OnResizeGripUp(object sender, MouseButtonEventArgs e)
        {
            EndResize();
            e.Handled = true;
        }

        private void EndResize()
        {
            if (!_resizing) return;
            _resizing = false;
            if (_resizeGrip.IsMouseCaptured) _resizeGrip.ReleaseMouseCapture();
            _state.WindowWidth = Width;
            _state.WindowHeight = Height;
            SaveWindowPosition();
            if (_options.DevMode) _store.Log("[debug] resize end " + Width.ToString("0") + "x" + Height.ToString("0"), null);
        }

        private void SetWindowSize(double width, double height)
        {
            Rect work = SystemParameters.WorkArea;
            Width = Math.Min(Math.Max(330, width), Math.Max(330, work.Right - Left - 6));
            Height = Math.Min(Math.Max(460, height), Math.Max(460, work.Bottom - Top - 6));
            _state.WindowWidth = Width;
            _state.WindowHeight = Height;
            SaveWindowPosition();
        }
    }
}
