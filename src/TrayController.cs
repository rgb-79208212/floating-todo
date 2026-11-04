using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FloatingTodo
{
    public class TrayController : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _showItem;
        private ToolStripMenuItem _hideItem;
        private ToolStripMenuItem _quitItem;

        private Icon _normalIcon;
        private Icon _alertIcon;
        private Icon _currentIcon;
        private bool _disposed;
        private bool _urgent;

        public event EventHandler ShowRequested;
        public event EventHandler HideRequested;
        public event EventHandler QuitRequested;

        public TrayController()
        {
            BuildMenu();
            _normalIcon = LoadIcon("FloatingTodo.app.ico");
            _alertIcon = LoadIcon("FloatingTodo.app.alert.ico");
            if (_normalIcon == null) _normalIcon = (Icon)SystemIcons.Application.Clone();
            if (_alertIcon == null) _alertIcon = (Icon)_normalIcon.Clone();
            _currentIcon = _normalIcon;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = _currentIcon;
            _notifyIcon.Text = "浮光待办";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = _menu;
            _notifyIcon.MouseClick += OnTrayMouseClick;
            _notifyIcon.BalloonTipClicked += delegate
            {
                if (ShowRequested != null) ShowRequested(this, EventArgs.Empty);
            };
        }

        private void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            _menu.Width = 198;
            _menu.ShowImageMargin = false;
            _menu.ShowCheckMargin = false;
            _menu.BackColor = Color.FromArgb(30, 31, 40);
            _menu.ForeColor = Color.FromArgb(242, 238, 248);
            _menu.Font = new Font("Microsoft YaHei UI", 9.2f, FontStyle.Regular);
            _menu.Renderer = new ToolStripProfessionalRenderer(new DarkMenuColorTable());
            _menu.Padding = new Padding(3, 5, 3, 5);

            _showItem = new ToolStripMenuItem("显示浮光待办");
            _showItem.Font = new Font("Microsoft YaHei UI", 9.2f, FontStyle.Bold);
            _showItem.Padding = new Padding(0, 5, 0, 5);
            _showItem.Click += delegate { Fire(ShowRequested); };
            _menu.Items.Add(_showItem);

            _hideItem = new ToolStripMenuItem("收进任务栏");
            _hideItem.Padding = new Padding(0, 4, 0, 4);
            _hideItem.Click += delegate { Fire(HideRequested); };
            _menu.Items.Add(_hideItem);

            _menu.Items.Add(new ToolStripSeparator());

            _quitItem = new ToolStripMenuItem("退出浮光待办…");
            _quitItem.ForeColor = Color.FromArgb(255, 154, 162);
            _quitItem.Padding = new Padding(0, 4, 0, 4);
            _quitItem.Click += delegate { Fire(QuitRequested); };
            _menu.Items.Add(_quitItem);
        }

        private void Fire(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnTrayMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (ShowRequested != null) ShowRequested(this, EventArgs.Empty);
            }
        }

        public void SetUrgent(bool urgent)
        {
            if (_disposed) return;
            _urgent = urgent;
            Icon wanted = urgent ? _alertIcon : _normalIcon;
            if (!object.ReferenceEquals(_currentIcon, wanted))
            {
                _currentIcon = wanted;
                _notifyIcon.Icon = wanted;
            }
        }

        public void SetTooltip(string text)
        {
            if (_disposed || _notifyIcon == null) return;
            if (text == null) text = "浮光待办";
            if (text.Length > 63) text = text.Substring(0, 61) + "…";
            try { _notifyIcon.Text = text; } catch { }
        }

        public void ShowInfo(string title, string message)
        {
            if (_disposed) return;
            try
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                _notifyIcon.ShowBalloonTip(3800);
            }
            catch { }
        }

        public void ShowMenuAtCursor()
        {
            if (_disposed) return;
            _menu.Show(Cursor.Position);
        }

        private static Icon LoadIcon(string resourceName)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    using (Icon icon = new Icon(stream))
                    {
                        return (Icon)icon.Clone();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            if (_menu != null)
            {
                _menu.Dispose();
                _menu = null;
            }
            if (_currentIcon != null) _currentIcon = null;
            if (_normalIcon != null) { _normalIcon.Dispose(); _normalIcon = null; }
            if (_alertIcon != null) { _alertIcon.Dispose(); _alertIcon = null; }
        }

        private class DarkMenuColorTable : ProfessionalColorTable
        {
            private static readonly Color Bg = Color.FromArgb(30, 31, 40);
            private static readonly Color Hover = Color.FromArgb(61, 55, 79);
            private static readonly Color Border = Color.FromArgb(58, 58, 72);

            public override Color ToolStripDropDownBackground { get { return Bg; } }
            public override Color ImageMarginGradientBegin { get { return Bg; } }
            public override Color ImageMarginGradientMiddle { get { return Bg; } }
            public override Color ImageMarginGradientEnd { get { return Bg; } }
            public override Color MenuBorder { get { return Border; } }
            public override Color MenuItemBorder { get { return Hover; } }
            public override Color MenuItemSelected { get { return Hover; } }
            public override Color MenuItemSelectedGradientBegin { get { return Hover; } }
            public override Color MenuItemSelectedGradientEnd { get { return Hover; } }
            public override Color MenuItemPressedGradientBegin { get { return Hover; } }
            public override Color MenuItemPressedGradientEnd { get { return Hover; } }
            public override Color SeparatorDark { get { return Border; } }
            public override Color SeparatorLight { get { return Color.FromArgb(18, 255, 255, 255); } }
            public override Color CheckBackground { get { return Color.Transparent; } }
        }
    }

}
