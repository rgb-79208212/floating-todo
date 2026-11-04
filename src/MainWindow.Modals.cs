using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FloatingTodo
{
    public partial class MainWindow
    {
        private void ShowConfirmModal(string title, string body, string confirmLabel, Action action)
        {
            ShowModalCore(title, body, confirmLabel, false, action);
        }

        private void ShowQuitModal()
        {
            if (_modalOpen) return;
            ShowModalCore(LanguageManager.T("退出浮光待办？"),
                LanguageManager.T("退出后小组件会关闭。只要仍开启「开机自动显示」，下次登录仍会出现。"),
                LanguageManager.T("退出"), true, delegate
                {
                    bool alsoDisable = _modalCheck.IsChecked == true;
                    if (alsoDisable) SetAutoStart(false);
                    QuitImmediately();
                });
            try
            {
                _modalIcon.Data = Geometry.Parse("M8,2.2 L8,8.1 M4.2,4.5 C2.9,5.8 2.3,7.6 2.5,9.4 C2.9,12.4 5.4,14.6 8.5,14.6 C11.6,14.6 14.2,12.4 14.6,9.4 C14.8,7.6 14.1,5.8 12.8,4.5");
                _modalIcon.StrokeThickness = 1.3;
            }
            catch { }
        }

        private void ShowModalCore(string title, string body, string confirmLabel, bool showAutoStartCheck, Action action)
        {
            CloseTransientPopups();
            _modalOpen = true;
            _modalIsQuit = showAutoStartCheck;
            _modalAction = action;
            _modalTitle.Text = title;
            _modalBody.Text = body;
            _modalConfirmButton.Content = confirmLabel;
            _modalCheck.Visibility = showAutoStartCheck ? Visibility.Visible : Visibility.Collapsed;
            _modalCheck.IsChecked = false;
            _modalIcon.Data = Geometry.Parse("M5,5 L11,11 M11,5 L5,11");
            _modalIcon.StrokeThickness = 1.6;

            _modalOverlay.Visibility = Visibility.Visible;
            _modalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            _modalOverlay.Opacity = 0;
            _modalCard.BeginAnimation(UIElement.OpacityProperty, null);
            _modalCard.Opacity = 0;
            ScaleTransform scale = _modalCard.RenderTransform as ScaleTransform;
            if (scale == null) { scale = new ScaleTransform(0.97, 0.97); _modalCard.RenderTransform = scale; }
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = 0.97;
            scale.ScaleY = 0.97;
            _modalOverlay.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)));
            _modalCard.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)));
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            DoubleAnimation sx = new DoubleAnimation(0.97, 1, TimeSpan.FromMilliseconds(180));
            sx.EasingFunction = ease;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, sx);
        }

        private void HideModal()
        {
            if (!_modalOpen) return;
            _modalOpen = false;
            DoubleAnimation fade = new DoubleAnimation(_modalOverlay.Opacity, 0, TimeSpan.FromMilliseconds(110));
            fade.Completed += delegate
            {
                _modalOverlay.Visibility = Visibility.Collapsed;
                _modalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
                _modalOverlay.Opacity = 0;
                _modalAction = null;
            };
            _modalOverlay.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void ConfirmModal()
        {
            if (!_modalOpen) return;
            Action action = _modalAction;
            _modalOpen = false;
            _modalOverlay.Visibility = Visibility.Collapsed;
            _modalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            _modalAction = null;
            if (action != null)
            {
                try { action(); }
                catch (Exception ex) { _store.Log("确认操作失败。", ex); }
            }
        }
    }
}
