using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Path = System.Windows.Shapes.Path;

namespace FloatingTodo
{
    public partial class MainWindow : Window
    {
        private const double CardWidth = 372;
        private const double DesiredHeight = 566;
        private const double CardRadius = 28;

        private StateStore _store;
        private AppState _state;
        private StartupOptions _options;
        private TrayController _tray;
        private HwndSource _source;
        private readonly uint _showMessage;
        private readonly uint _quitMessage;

        private Grid _root;
        private Border _glassCard;
        private Grid _headerArea;
        private Border _inputBar;
        private TextBox _inputBox;
        private TextBlock _inputPlaceholder;
        private TextBlock _dateText;
        private TextBlock _weekdayText;
        private TextBlock _summaryText;
        private TextBlock _activeCountText;
        private TextBlock _progressText;
        private Border _progressFill;
        private Border _pendingDueRow;
        private Button _pendingDueButton;
        private TextBlock _pendingDueText;
        private Button _clearPendingDueButton;
        private Button _selectModeButton;
        private TextBlock _selectModeText;
        private Grid _normalFooter;
        private Grid _batchBar;
        private TextBlock _selectionCountText;
        private Button _selectAllButton;
        private TextBlock _selectAllText;
        private Button _batchDoneButton;
        private Button _batchDueButton;
        private Button _batchDeleteButton;
        private ScrollViewer _taskScroll;
        private StackPanel _activeListPanel;
        private Border _emptyState;
        private Grid _completedHost;
        private StackPanel _completedListPanel;
        private Button _completedToggleButton;
        private TextBlock _completedCountText;
        private Path _completedChevron;
        private Button _clearCompletedButton;
        private Button _moreButton;
        private Button _themeToggleButton;
        private Path _themeToggleIcon;
        private Button _minimizeButton;
        private Button _closeButton;
        private Button _addButton;
        private Button _dueButton;
        private ScrollViewer _tagFilterScroll;
        private StackPanel _tagFilterPanel;
        private Grid _resizeGrip;
        private Grid _editorOverlay;
        private Border _editorCard;
        private TextBox _editorTitleBox;
        private TextBox _editorNoteBox;
        private TextBox _editorNewTagBox;
        private WrapPanel _editorTagPanel;
        private Button _editorAddTagButton;
        private Button _editorDueButton;
        private Button _editorSaveButton;
        private Button _editorCancelButton;
        private Button _editorCloseButton;
        private TextBlock _editorErrorText;
        private Border _modalCard;
        private Grid _modalOverlay;
        private TextBlock _modalTitle;
        private TextBlock _modalBody;
        private CheckBox _modalCheck;
        private Path _modalIcon;
        private Button _modalCancelButton;
        private Button _modalConfirmButton;
        private Popup _menuPopup;
        private StackPanel _moreMenuPanel;
        private Popup _taskMenuPopup;
        private StackPanel _taskMenuPanel;
        private Popup _duePopup;
        private TextBlock _dueMonthLabel;
        private TextBlock _dueYearRangeLabel;
        private TextBlock _dueErrorText;
        private Button _duePrevButton;
        private Button _dueNextButton;
        private Button _dueMonthButton;
        private Button _dueYearPrevButton;
        private Button _dueYearNextButton;
        private Button _dueCloseButton;
        private Button _dueClearButton;
        private Button _dueCancelButton;
        private Button _dueApplyButton;
        private StackPanel _dueCalendarArea;
        private StackPanel _dueYearMonthPanel;
        private UniformGrid _dueDayGrid;
        private UniformGrid _dueYearGrid;
        private UniformGrid _dueMonthGrid;
        private Button _dueHourButton;
        private Button _dueMinuteButton;
        private TextBlock _dueHourText;
        private TextBlock _dueMinuteText;
        private TextBlock _dueTimeDropdownHint;
        private ScrollViewer _dueTimeDropdownScroll;
        private StackPanel _dueTimeDropdownList;

        private DispatcherTimer _clockTimer;
        private DispatcherTimer _positionSaveTimer;
        private DispatcherTimer _revealTimer;

        private bool _quitting;
        private bool _readyForPositionSave;
        private bool _hideTipShown;
        private bool _initialRevealDone;
        private bool _modalOpen;
        private bool _modalIsQuit;
        private Action _modalAction;
        private string _lastRenderedMinute = "";
        private DateTime _lastRenderedDate = DateTime.MinValue;
        private readonly HashSet<string> _balloonedUrgent = new HashSet<string>();
        private string _autoStartError;

        private DateTime? _pendingDue;
        private Action<DateTime?> _dueApply;
        private DateTime _calendarMonth;
        private DateTime? _selectedDate;
        private bool _dueAllowClear;
        private bool _monthYearMode;
        private int _selectedHour;
        private int _selectedMinute;
        private int _pickerYear;
        private int _yearRangeStart;
        private UIElement _dueAnchor;
        private bool _timeDropdownIsHour;

        private bool _selectionMode;
        private readonly HashSet<string> _selectedTaskIds = new HashSet<string>(StringComparer.Ordinal);
        private TodoTask _contextTask;
        private UIElement _contextAnchor;

        private bool _inlineEditing;
        private bool _finishingInline;
        private TodoTask _editingTask;
        private TaskRowView _editingView;
        private string _newRowId;

        private bool _dragScrolling;
        private bool _dragPending;
        private Point _dragStartPoint;
        private double _dragStartOffset;
        private bool _suppressTaskClick;

        private bool _resizing;
        private Point _resizeStartScreen;
        private double _resizeStartWidth;
        private double _resizeStartHeight;

        private TodoTask _editingTaskDetail;
        private string _activeTagFilter;
        private List<string> _editorSelectedTags;
        private DateTime? _editorPendingDue;
        private bool _editorDueChanged;

        public MainWindow(StateStore store, StartupOptions options, uint showMessage, uint quitMessage)
        {
            _store = store;
            _state = store.State;
            _options = options;
            _showMessage = showMessage;
            _quitMessage = quitMessage;
            ThemeManager.Initialize(_state.Settings.Theme);

            Title = "浮光待办";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            ShowInTaskbar = false;
            Topmost = _state.Settings.AlwaysOnTop;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Background = Brushes.Transparent;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            FontFamily = new FontFamily("Microsoft YaHei UI");
            double workWidth = SystemParameters.WorkArea.Width;
            double workHeight = SystemParameters.WorkArea.Height;
            Width = Math.Min(Math.Max(330, _state.WindowWidth), Math.Max(330, workWidth - 20));
            Height = Math.Min(Math.Max(460, _state.WindowHeight), Math.Max(460, workHeight - 20));

            LoadInterface();
            FindParts();
            ApplyThemeSurface();
            ApplyIcon();
            WireEvents();
            PositionWindow();
            BuildTray();
            ApplyWindowSettings();
            UpdateThemeToggle();
            UpdateHeader();
            UpdatePendingDueUi();
            RebuildAll(false);
            _readyForPositionSave = true;

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            LocationChanged += OnLocationChanged;
            PreviewKeyDown += OnWindowPreviewKeyDown;
            Application.Current.SessionEnding += OnSessionEnding;
        }

        public void ShowInitial()
        {
            if (IsVisible) return;
            ShowActivated = !_options.AutoStartLaunch;
            _root.Opacity = 0;
            Show();
            ScheduleInitialReveal();
        }

        private void LoadInterface()
        {
            string xaml = ThemeManager.TransformXaml(ReadTextResource("FloatingTodo.UI.xaml"));
            _root = (Grid)XamlReader.Parse(xaml);
            Content = _root;
            _root.Opacity = 0;
        }

        private void FindParts()
        {
            _glassCard = Find<Border>("GlassCard");
            _headerArea = Find<Grid>("HeaderArea");
            _inputBar = Find<Border>("InputBar");
            _inputBox = Find<TextBox>("InputBox");
            _inputPlaceholder = Find<TextBlock>("InputPlaceholder");
            _dateText = Find<TextBlock>("DateText");
            _weekdayText = Find<TextBlock>("WeekdayText");
            _summaryText = Find<TextBlock>("SummaryText");
            _activeCountText = Find<TextBlock>("ActiveCountText");
            _progressText = Find<TextBlock>("ProgressText");
            _progressFill = Find<Border>("ProgressFill");
            _pendingDueRow = Find<Border>("PendingDueRow");
            _pendingDueButton = Find<Button>("PendingDueButton");
            _pendingDueText = Find<TextBlock>("PendingDueText");
            _clearPendingDueButton = Find<Button>("ClearPendingDueButton");
            _selectModeButton = Find<Button>("SelectModeButton");
            _selectModeText = Find<TextBlock>("SelectModeText");
            _normalFooter = Find<Grid>("NormalFooter");
            _batchBar = Find<Grid>("BatchBar");
            _selectionCountText = Find<TextBlock>("SelectionCountText");
            _selectAllButton = Find<Button>("SelectAllButton");
            _selectAllText = Find<TextBlock>("SelectAllText");
            _batchDoneButton = Find<Button>("BatchDoneButton");
            _batchDueButton = Find<Button>("BatchDueButton");
            _batchDeleteButton = Find<Button>("BatchDeleteButton");
            _taskScroll = Find<ScrollViewer>("TaskScroll");
            _activeListPanel = Find<StackPanel>("ActiveListPanel");
            _emptyState = Find<Border>("EmptyState");
            _completedHost = Find<Grid>("CompletedHost");
            _completedListPanel = Find<StackPanel>("CompletedListPanel");
            _completedToggleButton = Find<Button>("CompletedToggleButton");
            _completedCountText = Find<TextBlock>("CompletedCountText");
            _completedChevron = Find<Path>("CompletedChevron");
            _clearCompletedButton = Find<Button>("ClearCompletedButton");
            _moreButton = Find<Button>("MoreButton");
            _themeToggleButton = Find<Button>("ThemeToggleButton");
            _themeToggleIcon = Find<Path>("ThemeToggleIcon");
            _minimizeButton = Find<Button>("MinimizeButton");
            _closeButton = Find<Button>("CloseButton");
            _addButton = Find<Button>("AddButton");
            _dueButton = Find<Button>("DueButton");
            _tagFilterScroll = Find<ScrollViewer>("TagFilterScroll");
            _tagFilterPanel = Find<StackPanel>("TagFilterPanel");
            _resizeGrip = Find<Grid>("ResizeGrip");
            _editorOverlay = Find<Grid>("EditorOverlay");
            _editorCard = Find<Border>("EditorCard");
            _editorTitleBox = Find<TextBox>("EditorTitleBox");
            _editorNoteBox = Find<TextBox>("EditorNoteBox");
            _editorNewTagBox = Find<TextBox>("EditorNewTagBox");
            _editorTagPanel = Find<WrapPanel>("EditorTagPanel");
            _editorAddTagButton = Find<Button>("EditorAddTagButton");
            _editorDueButton = Find<Button>("EditorDueButton");
            _editorSaveButton = Find<Button>("EditorSaveButton");
            _editorCancelButton = Find<Button>("EditorCancelButton");
            _editorCloseButton = Find<Button>("EditorCloseButton");
            _editorErrorText = Find<TextBlock>("EditorErrorText");
            _modalOverlay = Find<Grid>("ModalOverlay");
            _modalCard = Find<Border>("ModalCard");
            _modalTitle = Find<TextBlock>("ModalTitle");
            _modalBody = Find<TextBlock>("ModalBody");
            _modalCheck = Find<CheckBox>("ModalCheck");
            _modalIcon = Find<Path>("ModalIcon");
            _modalCancelButton = Find<Button>("ModalCancelButton");
            _modalConfirmButton = Find<Button>("ModalConfirmButton");
            _menuPopup = Find<Popup>("MenuPopup");
            _moreMenuPanel = Find<StackPanel>("MoreMenuPanel");
            _taskMenuPopup = Find<Popup>("TaskMenuPopup");
            _taskMenuPanel = Find<StackPanel>("TaskMenuPanel");
            _duePopup = Find<Popup>("DuePopup");
            _dueMonthLabel = Find<TextBlock>("DueMonthLabel");
            _dueYearRangeLabel = Find<TextBlock>("DueYearRangeLabel");
            _dueErrorText = Find<TextBlock>("DueErrorText");
            _duePrevButton = Find<Button>("DuePrevButton");
            _dueNextButton = Find<Button>("DueNextButton");
            _dueMonthButton = Find<Button>("DueMonthButton");
            _dueYearPrevButton = Find<Button>("DueYearPrevButton");
            _dueYearNextButton = Find<Button>("DueYearNextButton");
            _dueCloseButton = Find<Button>("DueCloseButton");
            _dueClearButton = Find<Button>("DueClearButton");
            _dueCancelButton = Find<Button>("DueCancelButton");
            _dueApplyButton = Find<Button>("DueApplyButton");
            _dueCalendarArea = Find<StackPanel>("DueCalendarArea");
            _dueYearMonthPanel = Find<StackPanel>("DueYearMonthPanel");
            _dueDayGrid = Find<UniformGrid>("DueDayGrid");
            _dueYearGrid = Find<UniformGrid>("DueYearGrid");
            _dueMonthGrid = Find<UniformGrid>("DueMonthGrid");
            _dueHourButton = Find<Button>("DueHourButton");
            _dueMinuteButton = Find<Button>("DueMinuteButton");
            _dueHourText = Find<TextBlock>("DueHourText");
            _dueMinuteText = Find<TextBlock>("DueMinuteText");
            _dueTimeDropdownHint = Find<TextBlock>("DueTimeDropdownHint");
            _dueTimeDropdownScroll = Find<ScrollViewer>("DueTimeDropdownScroll");
            _dueTimeDropdownList = Find<StackPanel>("DueTimeDropdownList");

            if (_root == null || _glassCard == null || _inputBox == null || _activeListPanel == null ||
                _themeToggleButton == null || _themeToggleIcon == null ||
                _duePopup == null || _dueDayGrid == null || _menuPopup == null || _batchBar == null)
            {
                throw new InvalidOperationException("界面资源加载不完整。");
            }
        }

        private T Find<T>(string name) where T : class
        {
            if (_root == null) return null;
            return _root.FindName(name) as T;
        }

        private void ApplyIcon()
        {
            try
            {
                string exe = Assembly.GetExecutingAssembly().Location;
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(exe))
                {
                    if (icon != null)
                    {
                        this.Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch { }
        }

        private void WireEvents()
        {
            _addButton.Click += delegate { AddTaskFromInput(); };
            _dueButton.Click += delegate { OpenDueEditorForPending(); };
            _pendingDueButton.Click += delegate { OpenDueEditorForPending(); };
            _clearPendingDueButton.Click += delegate { _pendingDue = null; UpdatePendingDueUi(); };
            _moreButton.Click += delegate { OpenMoreMenu(); };
            _themeToggleButton.Click += delegate { ToggleAppearance(); };
            _minimizeButton.Click += delegate { HideToTray(); };
            _closeButton.Click += delegate { QuitImmediately(); };
            _selectModeButton.Click += delegate { ToggleSelectionMode(); };
            _selectAllButton.Click += delegate { ToggleSelectAll(); };
            _batchDoneButton.Click += delegate { ConfirmBatchDone(); };
            _batchDueButton.Click += delegate { OpenDueEditorForSelection(); };
            _batchDeleteButton.Click += delegate { ConfirmBatchDelete(); };
            _completedToggleButton.Click += delegate { ToggleCompletedSection(); };
            _clearCompletedButton.Click += delegate { ConfirmClearCompleted(); };
            _modalCancelButton.Click += delegate { HideModal(); };
            _modalConfirmButton.Click += delegate { ConfirmModal(); };
            _inputBox.TextChanged += delegate { UpdateInputPlaceholder(); };
            _inputBox.GotKeyboardFocus += delegate { SetInputFocusVisual(true); };
            _inputBox.LostKeyboardFocus += delegate { SetInputFocusVisual(false); };
            _inputBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    AddTaskFromInput();
                    e.Handled = true;
                }
            };
            _inputBar.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) == null) _inputBox.Focus();
            };
            _headerArea.MouseLeftButtonDown += OnHeaderMouseDown;
            _headerArea.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e)
            {
                OpenMoreMenu();
                e.Handled = true;
            };
            _duePrevButton.Click += delegate { _calendarMonth = _calendarMonth.AddMonths(-1); BuildCalendar(); };
            _dueNextButton.Click += delegate { _calendarMonth = _calendarMonth.AddMonths(1); BuildCalendar(); };
            _dueYearPrevButton.Click += delegate { _yearRangeStart -= 12; BuildYearMonthGrids(); };
            _dueYearNextButton.Click += delegate { _yearRangeStart += 12; BuildYearMonthGrids(); };
            _dueMonthButton.Click += delegate { ToggleMonthYearMode(); };
            _dueCloseButton.Click += delegate { _duePopup.IsOpen = false; };
            _dueCancelButton.Click += delegate { _duePopup.IsOpen = false; };
            _dueApplyButton.Click += delegate { ApplyDueEditor(); };
            _dueClearButton.Click += delegate
            {
                Action<DateTime?> apply = _dueApply;
                _duePopup.IsOpen = false;
                if (apply != null) apply(null);
            };
            _duePopup.Closed += delegate { _dueApply = null; };
            _dueHourButton.Click += delegate { ShowTimeDropdown(true); };
            _dueMinuteButton.Click += delegate { ShowTimeDropdown(false); };
            _editorCloseButton.Click += delegate { CloseTaskEditor(); };
            _editorCancelButton.Click += delegate { CloseTaskEditor(); };
            _editorSaveButton.Click += delegate { SaveTaskEditor(); };
            _editorAddTagButton.Click += delegate { AddEditorTag(); };
            _editorNewTagBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { AddEditorTag(); e.Handled = true; }
            };
            _editorDueButton.Click += delegate { OpenDueEditorForEditor(); };
            _resizeGrip.MouseLeftButtonDown += OnResizeGripDown;
            _resizeGrip.MouseMove += OnResizeGripMove;
            _resizeGrip.MouseLeftButtonUp += OnResizeGripUp;
            _resizeGrip.LostMouseCapture += delegate { EndResize(); };
            _taskScroll.PreviewMouseLeftButtonDown += OnListMouseDown;
            _taskScroll.PreviewMouseMove += OnListMouseMove;
            _taskScroll.PreviewMouseLeftButtonUp += OnListMouseUp;
            _taskScroll.LostMouseCapture += delegate { EndListDrag(); };
            _taskScroll.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.PageDown) { _taskScroll.PageDown(); e.Handled = true; }
                else if (e.Key == Key.PageUp) { _taskScroll.PageUp(); e.Handled = true; }
            };
        }

        private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (_state.Settings.DesktopPinned) return;
            if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null) return;
            if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null) return;
            try { DragMove(); } catch { }
        }

        private static T FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            DependencyObject node = start;
            while (node != null)
            {
                T match = node as T;
                if (match != null) return match;
                node = VisualTreeHelper.GetParent(node);
            }
            return null;
        }

        private void ToggleAppearance()
        {
            if (_quitting) return;
            ThemeMode next = ThemeManager.IsLight ? ThemeMode.Dark : ThemeMode.Light;
            _state.Settings.Theme = next == ThemeMode.Dark ? "dark" : "light";
            _store.SaveAsync();
            ThemeManager.SetMode(next);
            ReloadInterfaceForTheme();
        }

        private void ReloadInterfaceForTheme()
        {
            CloseTransientPopups();
            if (_modalOpen) HideModal();
            if (_editorOverlay != null && _editorOverlay.Visibility == Visibility.Visible)
            {
                _editorOverlay.Visibility = Visibility.Collapsed;
                _editingTaskDetail = null;
            }
            _selectionMode = false;
            _selectedTaskIds.Clear();
            _inlineEditing = false;

            LoadInterface();
            FindParts();
            ApplyThemeSurface();
            WireEvents();
            ApplyWindowSettings();
            UpdateThemeToggle();
            UpdateHeader();
            UpdatePendingDueUi();
            RebuildAll(true);
            _root.Opacity = 1;
            Dispatcher.BeginInvoke(new Action(FocusInput), DispatcherPriority.Input);
        }

        private void ApplyThemeSurface()
        {
            if (_glassCard != null)
            {
                _glassCard.CornerRadius = new CornerRadius(26);
                _glassCard.Background = ThemeManager.Palette.CardGradient();
                _glassCard.BorderBrush = ThemeManager.Brush(ThemeRole.CardBorder);
            }
            if (_inputBar != null)
            {
                _inputBar.CornerRadius = new CornerRadius(15);
                _inputBar.Background = ThemeManager.Brush(ThemeRole.Input);
                _inputBar.BorderBrush = ThemeManager.Brush(ThemeRole.InputBorder);
                _inputBar.BorderThickness = new Thickness(0);
            }
            if (_dateText != null) _dateText.Foreground = ThemeManager.Brush(ThemeRole.TextPrimary);
            if (_weekdayText != null) _weekdayText.Foreground = ThemeManager.Brush(ThemeRole.TextSecondary);
            if (_summaryText != null) _summaryText.Foreground = ThemeManager.Brush(ThemeRole.TextSecondary);
            if (_inputPlaceholder != null) _inputPlaceholder.Foreground =
                ThemeManager.Brush(ThemeManager.IsLight ? ThemeRole.TextMuted : ThemeRole.TimeSub);
            if (_activeCountText != null) _activeCountText.Foreground = ThemeManager.Brush(ThemeRole.TextSecondary);
            if (_progressText != null) _progressText.Foreground = ThemeManager.Brush(ThemeRole.TextSecondary);
            if (_selectModeText != null) _selectModeText.Foreground = ThemeManager.Brush(ThemeRole.TextMuted);
            if (_themeToggleIcon != null) _themeToggleIcon.Stroke = ThemeManager.Brush(ThemeRole.TextPrimary);
            if (_themeToggleButton != null)
            {
                _themeToggleButton.Background = ThemeManager.Brush(ThemeRole.Control);
                _themeToggleButton.BorderBrush = ThemeManager.Brush(ThemeRole.ControlBorder);
                _moreButton.Background = ThemeManager.Brush(ThemeRole.Control);
                _moreButton.BorderBrush = ThemeManager.Brush(ThemeRole.ControlBorder);
                _minimizeButton.Background = ThemeManager.Brush(ThemeRole.Control);
                _minimizeButton.BorderBrush = ThemeManager.Brush(ThemeRole.ControlBorder);
                _closeButton.Background = ThemeManager.Brush(ThemeRole.Control);
                _closeButton.BorderBrush = ThemeManager.Brush(ThemeRole.ControlBorder);
            }
            if (_inputBox != null)
            {
                _inputBox.Foreground = ThemeManager.Brush(ThemeRole.TextPrimary);
                _inputBox.CaretBrush = ThemeManager.Brush(ThemeRole.Accent);
            }
        }

        private void UpdateThemeToggle()
        {
            if (_themeToggleButton == null || _themeToggleIcon == null) return;
            bool light = ThemeManager.IsLight;
            _themeToggleIcon.Data = Geometry.Parse(light
                ? "M8,5.2 A2.8,2.8 0 1 1 7.99,5.2 M8,1.4 L8,3 M8,13 L8,14.6 M1.4,8 L3,8 M13,8 L14.6,8 M3.4,3.4 L4.5,4.5 M11.5,11.5 L12.6,12.6 M12.6,3.4 L11.5,4.5 M4.5,11.5 L3.4,12.6"
                : "M12.7 9.7 A5.1 5.1 0 0 1 6.3 3.3 A5.15 5.15 0 1 0 12.7 9.7 Z");
            _themeToggleButton.ToolTip = light ? "切换到深色" : "切换到浅色";
            AutomationProperties.SetName(_themeToggleButton, light ? "切换到深色" : "切换到浅色");
        }

        private void BuildTray()
        {
            _tray = new TrayController();
            _tray.ShowRequested += delegate { ShowFromTray(true); };
            _tray.HideRequested += delegate { HideToTray(); };
            _tray.QuitRequested += delegate { QuitImmediately(); };
            UpdateTrayState();
        }

        private void ScheduleInitialReveal()
        {
            if (_initialRevealDone) return;
            _initialRevealDone = true;
            _revealTimer = new DispatcherTimer();
            _revealTimer.Interval = TimeSpan.FromMilliseconds(240);
            _revealTimer.Tick += delegate
            {
                _revealTimer.Stop();
                if (!IsVisible) return;
                RevealContent();
                if (!_options.AutoStartLaunch) FocusInput();
            };
            _revealTimer.Start();
        }

        private void RevealContent()
        {
            DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(230));
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            _root.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_options.DevMode)
            {
                Dispatcher.BeginInvoke(new Action(EnsureInitialAutoStart), DispatcherPriority.Background);
            }

            if (!_initialRevealDone) ScheduleInitialReveal();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(20);
            _clockTimer.Tick += delegate { OnClockTick(); };
            _clockTimer.Start();

            _positionSaveTimer = new DispatcherTimer();
            _positionSaveTimer.Interval = TimeSpan.FromMilliseconds(500);
            _positionSaveTimer.Tick += delegate
            {
                _positionSaveTimer.Stop();
                SaveWindowPosition();
            };
            UpdateHeader();
            RebuildAll(false);
        }

        private void EnsureInitialAutoStart()
        {
            string error;
            AutoStartManager.TrySetEnabled(_state.Settings.AutoStart, Assembly.GetExecutingAssembly().Location, out error);
            if (!string.IsNullOrEmpty(error)) _store.Log(error, null);
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(hwnd);
            if (_source != null) _source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == unchecked((int)_showMessage))
            {
                handled = true;
                Dispatcher.BeginInvoke(new Action(delegate { ShowFromTray(true); }), DispatcherPriority.Normal);
            }
            else if (message == unchecked((int)_quitMessage))
            {
                handled = true;
                Dispatcher.BeginInvoke(new Action(delegate { QuitImmediately(); }), DispatcherPriority.Normal);
            }
            return IntPtr.Zero;
        }

        private void PositionWindow()
        {
            Rect work = SystemParameters.WorkArea;
            double left = work.Right - Width - 18;
            double top = work.Top + 32;
            if (_state.HasWindowPosition)
            {
                left = _state.WindowLeft;
                top = _state.WindowTop;
            }
            double vLeft = SystemParameters.VirtualScreenLeft;
            double vTop = SystemParameters.VirtualScreenTop;
            double vRight = vLeft + SystemParameters.VirtualScreenWidth;
            double vBottom = vTop + SystemParameters.VirtualScreenHeight;
            left = Math.Max(vLeft - Width + 120, Math.Min(left, vRight - 120));
            top = Math.Max(vTop - Height + 90, Math.Min(top, vBottom - 48));
            Left = left;
            Top = top;
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            if (!_readyForPositionSave) return;
            SchedulePositionSave();
        }

        private void SchedulePositionSave()
        {
            if (!_readyForPositionSave || _positionSaveTimer == null) return;
            _positionSaveTimer.Stop();
            _positionSaveTimer.Start();
        }

        private void SaveWindowPosition()
        {
            if (WindowState != WindowState.Normal) return;
            _state.WindowLeft = Left;
            _state.WindowTop = Top;
            _state.WindowWidth = Width;
            _state.WindowHeight = Height;
            _state.HasWindowPosition = true;
            _store.SaveAsync();
        }

        private void ShowFromTray(bool focus)
        {
            if (_quitting) return;
            CloseTransientPopups();
            if (!IsVisible)
            {
                ShowActivated = focus;
                _root.Opacity = 0;
                Show();
                RevealContent();
                UpdateHeader();
                RebuildAll(false);
            }
            else
            {
                Topmost = _state.Settings.AlwaysOnTop;
                if (focus) Activate();
            }
            if (focus)
            {
                Activate();
                Dispatcher.BeginInvoke(new Action(FocusInput), DispatcherPriority.Input);
            }
        }

        private void HideToTray()
        {
            if (!IsVisible || _quitting) return;
            CloseTransientPopups();
            SaveWindowPosition();
            for (int i = 0; i < _state.Tasks.Count; i++)
            {
                TodoTask task = _state.Tasks[i];
                if (TaskService.IsUrgent(task, DateTime.Now, _state.Settings.LeadMinutes)) _balloonedUrgent.Add(task.Id);
            }
            Hide();
            if (_tray != null && !_hideTipShown)
            {
                _hideTipShown = true;
                _tray.ShowInfo("已收进系统托盘", "浮光待办仍在运行，单击右下角托盘图标即可恢复。");
            }
        }

        private void FocusInput()
        {
            if (!IsVisible) return;
            if (_inputBox != null)
            {
                _inputBox.Focus();
                Keyboard.Focus(_inputBox);
            }
        }

        private void AddTaskFromInput()
        {
            string title = TaskService.NormalizeTitle(_inputBox.Text);
            if (title.Length == 0)
            {
                ShakeInput();
                _inputBox.Focus();
                return;
            }
            TodoTask task = TaskService.Add(_state, title, _pendingDue, DateTime.Now);
            if (task == null) return;
            _store.SaveAsync();
            _inputBox.Text = "";
            _pendingDue = null;
            UpdatePendingDueUi();
            _newRowId = task.Id;
            RebuildAll(true);
            UpdateTrayState();
            Dispatcher.BeginInvoke(new Action(ScrollNewTaskIntoView), DispatcherPriority.Loaded);
            _inputBox.Focus();
        }

        private void ShakeInput()
        {
            TranslateTransform transform = new TranslateTransform();
            _inputBar.RenderTransform = transform;
            DoubleAnimationUsingKeyFrames shake = new DoubleAnimationUsingKeyFrames();
            shake.Duration = TimeSpan.FromMilliseconds(230);
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(95))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(-2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(145))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(210))));
            transform.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        private void UpdateInputPlaceholder()
        {
            if (_inputPlaceholder == null) return;
            _inputPlaceholder.Visibility = string.IsNullOrEmpty(_inputBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetInputFocusVisual(bool focused)
        {
            if (_inputBar == null) return;
            _inputBar.BorderBrush = focused
                ? new SolidColorBrush(Color.FromArgb(115, 188, 169, 255))
                : new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
            _inputBar.Background = focused
                ? new SolidColorBrush(Color.FromArgb(27, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(19, 255, 255, 255));
        }

        private void UpdatePendingDueUi()
        {
            if (_pendingDueRow == null) return;
            if (_pendingDue.HasValue)
            {
                _pendingDueRow.Visibility = Visibility.Visible;
                _pendingDueText.Text = "下一件 · " + TaskService.FormatDue(_pendingDue.Value, DateTime.Now);
                _dueButton.Background = new SolidColorBrush(Color.FromArgb(38, 188, 169, 255));
            }
            else
            {
                _pendingDueRow.Visibility = Visibility.Collapsed;
                _dueButton.Background = Brushes.Transparent;
            }
        }

        private void ApplyWindowSettings()
        {
            Topmost = _state.Settings.AlwaysOnTop;
            _headerArea.Cursor = _state.Settings.DesktopPinned ? Cursors.Arrow : Cursors.SizeAll;
            _headerArea.ToolTip = _state.Settings.DesktopPinned ? "位置已锁定，可在设置中解除" : null;
            if (_tray != null) _tray.SetUrgent(HasUrgentTasks());
        }

        private bool HasUrgentTasks()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < _state.Tasks.Count; i++)
            {
                if (TaskService.IsUrgent(_state.Tasks[i], now, _state.Settings.LeadMinutes)) return true;
            }
            return false;
        }

        private void SetDesktopPinned(bool pinned)
        {
            _state.Settings.DesktopPinned = pinned;
            _store.SaveAsync();
            ApplyWindowSettings();
        }

        private void SetAlwaysOnTop(bool onTop)
        {
            _state.Settings.AlwaysOnTop = onTop;
            _store.SaveAsync();
            ApplyWindowSettings();
        }

        private void SetAutoStart(bool enabled)
        {
            string error;
            bool okay = AutoStartManager.TrySetEnabled(enabled, Assembly.GetExecutingAssembly().Location, out error);
            if (okay)
            {
                _autoStartError = null;
                _state.Settings.AutoStart = enabled;
                _store.SaveAsync();
            }
            else
            {
                _autoStartError = error;
                _store.Log(error, null);
            }
        }

        private void OnClockTick()
        {
            DateTime now = DateTime.Now;
            if (now.Date != _lastRenderedDate) UpdateHeader();
            string minute = now.ToString("yyyyMMddHHmm");
            if (minute != _lastRenderedMinute && IsVisible && !_modalOpen && !_inlineEditing &&
                !_duePopup.IsOpen && !_selectionMode)
            {
                RebuildAll(true);
            }
            UpdateTrayState();
            NotifyHiddenUrgent();
        }

        private void UpdateHeader()
        {
            DateTime now = DateTime.Now;
            _lastRenderedDate = now.Date;
            if (_dateText != null) _dateText.Text = now.ToString("MM.dd", CultureInfo.InvariantCulture);
            if (_weekdayText != null)
            {
                string[] days = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
                string en = now.ToString("ddd", CultureInfo.InvariantCulture).ToUpperInvariant();
                _weekdayText.Text = days[(int)now.DayOfWeek] + " / " + en;
            }
            if (_summaryText != null) _summaryText.Text = TaskService.Summary(_state, now);

            int active = 0;
            int done = 0;
            for (int i = 0; i < _state.Tasks.Count; i++)
            {
                if (_state.Tasks[i].Done) done++; else active++;
            }
            if (_activeCountText != null) _activeCountText.Text = active.ToString();
            int total = active + done;
            if (_progressText != null) _progressText.Text = done.ToString() + " / " + total.ToString() + " 完成";
            if (_progressFill != null)
            {
                double ratio = total > 0 ? (double)done / total : 0;
                _progressFill.Width = ratio <= 0 ? 0 : Math.Max(4, 62 * ratio);
            }
            UpdateTrayState();
        }

        private void UpdateTrayState()
        {
            if (_tray == null) return;
            int active = 0;
            int urgent = 0;
            DateTime now = DateTime.Now;
            for (int i = 0; i < _state.Tasks.Count; i++)
            {
                TodoTask t = _state.Tasks[i];
                if (t.Done) continue;
                active++;
                if (TaskService.IsUrgent(t, now, _state.Settings.LeadMinutes)) urgent++;
            }
            _tray.SetUrgent(urgent > 0);
            string tip = "浮光待办 · " + active.ToString() + " 项待办";
            if (urgent > 0) tip += " · " + urgent.ToString() + " 项临近";
            _tray.SetTooltip(tip);
        }

        private void NotifyHiddenUrgent()
        {
            if (_tray == null || _quitting) return;
            if (IsVisible && WindowState != WindowState.Minimized) return;
            List<string> fresh = new List<string>();
            string firstName = null;
            DateTime now = DateTime.Now;
            for (int i = 0; i < _state.Tasks.Count; i++)
            {
                TodoTask t = _state.Tasks[i];
                if (t.Done || !t.DueAt.HasValue) continue;
                if (TaskService.IsUrgent(t, now, _state.Settings.LeadMinutes) && !_balloonedUrgent.Contains(t.Id))
                {
                    fresh.Add(t.Id);
                    if (firstName == null) firstName = t.Title;
                }
            }
            if (fresh.Count == 0) return;
            for (int i = 0; i < fresh.Count; i++) _balloonedUrgent.Add(fresh[i]);
            if (fresh.Count == 1) _tray.ShowInfo("待办临期", firstName);
            else _tray.ShowInfo("待办临期", fresh.Count.ToString() + " 项待办即将截止。");
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_modalOpen) { HideModal(); e.Handled = true; return; }
            if (_duePopup.IsOpen || _menuPopup.IsOpen || _taskMenuPopup.IsOpen)
            {
                CloseTransientPopups();
                e.Handled = true;
                return;
            }
            if (_editorOverlay != null && _editorOverlay.Visibility == Visibility.Visible)
            {
                CloseTaskEditor();
                e.Handled = true;
                return;
            }
            if (_selectionMode) { ExitSelectionMode(); e.Handled = true; return; }
        }

        private void CloseTransientPopups()
        {
            if (_duePopup != null) _duePopup.IsOpen = false;
            if (_menuPopup != null) _menuPopup.IsOpen = false;
            if (_taskMenuPopup != null) _taskMenuPopup.IsOpen = false;
        }

        private void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            try
            {
                _quitting = true;
                SaveWindowPosition();
                _store.SaveNow();
                if (_tray != null) _tray.Dispose();
            }
            catch { }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_quitting)
            {
                _quitting = true;
                try
                {
                    SaveWindowPosition();
                    _store.SaveNow();
                    if (_tray != null) { _tray.Dispose(); _tray = null; }
                }
                catch { }
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                Application.Current.SessionEnding -= OnSessionEnding;
                if (_source != null) { _source.RemoveHook(WndProc); _source = null; }
                if (_clockTimer != null) _clockTimer.Stop();
                if (_positionSaveTimer != null) _positionSaveTimer.Stop();
                if (_revealTimer != null) _revealTimer.Stop();
                if (_tray != null) { _tray.Dispose(); _tray = null; }
            }
            catch { }
            base.OnClosed(e);
            Dispatcher.BeginInvoke(new Action(delegate
            {
                Application.Current.Shutdown();
            }), DispatcherPriority.Background);
        }

        private void QuitImmediately()
        {
            if (_quitting) return;
            _quitting = true;
            try
            {
                SaveWindowPosition();
                _store.SaveNow();
                CloseTransientPopups();
                if (_tray != null) { _tray.Dispose(); _tray = null; }
                Close();
            }
            catch { }
            Application.Current.Shutdown();
        }

        private static string ReadTextResource(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidOperationException("Missing resource: " + name);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true)) return reader.ReadToEnd();
            }
        }
    }
}
