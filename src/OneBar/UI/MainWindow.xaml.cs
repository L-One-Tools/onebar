using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OneBar.Models;
using OneBar.Services;
using WinForms = System.Windows.Forms;

namespace OneBar.UI;

public partial class MainWindow : Window, IDisposable
{
    private const double CollapsedWidth = 260;
    private const double CollapsedHeight = 9;
    private const double ExpandedHeight = 62;
    private const double DragThreshold = 7;

    private readonly SettingsService _settingsService;
    private readonly ShellIntegrationService _shell;
    private readonly WorkflowService _workflowService = new();
    private readonly DispatcherTimer _collapseDelayTimer;
    private readonly DispatcherTimer _collapseAnimTimer;
    private readonly Stopwatch _collapseStopwatch = new();

    private AppSettings _settings;
    private IReadOnlyList<WorkflowDefinition> _workflows = Array.Empty<WorkflowDefinition>();
    private WorkflowDefinition? _activeWorkflow;
    private Button? _pinButton;
    private Button? _mouseDownButton;
    private Point _mouseDownPoint;
    private bool _suppressNextClick;
    private bool _isPinned;
    private bool _isDragging;
    private bool _isCollapsing;
    private int _collapseLevels;
    private bool _allowExit;
    private bool _settingsOpen;
    private readonly List<WorkflowDefinition> _path = new();
    private readonly Dictionary<string, Button> _categories = new();
    private BranchWindow? _branch;
    private WinForms.NotifyIcon? _trayIcon;
    private System.Drawing.Icon? _trayDrawingIcon;

    public MainWindow(SettingsService settingsService, ShellIntegrationService shell, AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _shell = shell;
        _settings = settings;

        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/OneBar.ico", UriKind.Absolute));
        }
        catch { }

        ShowInTaskbar = _settings.ShowTaskbarIcon;

        _collapseDelayTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(_settings.CollapseDelayMs)
        };
        _collapseDelayTimer.Tick += (_, _) =>
        {
            _collapseDelayTimer.Stop();
            StartCollapseAnimation();
        };

        _collapseAnimTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _collapseAnimTimer.Tick += CollapseAnimTimerOnTick;

        Closing += MainWindow_OnClosing;
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
        Deactivated += (_, _) =>
        {
            if (!_isPinned && !_isDragging) ScheduleCollapse();
        };
        Loaded += (_, _) =>
        {
            InitializeTray();
            ReloadWorkflows();
            BuildCatalogView();
            CollapseInstant();
        };
    }

    public void BringForward()
    {
        if (!IsVisible) Show();
        WindowState = WindowState.Normal;
        Activate();
        ExpandBar();
    }

    public void ShowSettings()
    {
        _collapseDelayTimer.Stop(); _collapseAnimTimer.Stop();
        _settingsOpen = true; _branch?.Hide();
        try
        {
            var window = new SettingsWindow(_settingsService, _shell, _settings) { Owner = this };
            if (window.ShowDialog() == true && window.UpdatedSettings is not null)
            {
                _settings = window.UpdatedSettings;
                ApplyRuntimeSettings(); ReloadWorkflows(); BuildCatalogView();
            }
        }
        finally { _settingsOpen = false; if (_path.Count > 0) RenderBranches(); }
    }

    public void Dispose()
    {
        _branch?.Close(); _branch = null;
        _collapseDelayTimer.Stop();
        _collapseAnimTimer.Stop();
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        _trayDrawingIcon?.Dispose();
        _trayDrawingIcon = null;
    }

    private void ReloadWorkflows()
    {
        _workflows = _workflowService.Load(_settings.WorkflowRoot);
        if (_activeWorkflow is not null)
        {
            _activeWorkflow = _workflows.FirstOrDefault(w =>
                string.Equals(w.Path, _activeWorkflow.Path, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void ApplyRuntimeSettings()
    {
        ShowInTaskbar = _settings.ShowTaskbarIcon;
        _collapseDelayTimer.Interval = TimeSpan.FromMilliseconds(_settings.CollapseDelayMs);
        if (_trayIcon is not null) _trayIcon.Visible = _settings.ShowTrayIcon;
    }

    private void InitializeTray()
    {
        try
        {
            var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/OneBar.ico", UriKind.Absolute));
            if (streamInfo?.Stream is not null)
            {
                using var sourceIcon = new System.Drawing.Icon(streamInfo.Stream);
                _trayDrawingIcon = (System.Drawing.Icon)sourceIcon.Clone();
            }
        }
        catch { }

        _trayIcon = new WinForms.NotifyIcon
        {
            Text = "OneBar",
            Icon = _trayDrawingIcon ?? System.Drawing.SystemIcons.Application,
            Visible = _settings.ShowTrayIcon
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("显示 OneBar", null, (_, _) => Dispatcher.Invoke(BringForward));
        menu.Items.Add("打开 Workflows", null, (_, _) => Dispatcher.Invoke(() => _shell.OpenWorkflowRoot(_settings.WorkflowRoot)));
        menu.Items.Add("设置", null, (_, _) => Dispatcher.Invoke(ShowSettings));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出 OneBar", null, (_, _) => Dispatcher.Invoke(ExitOneBar));
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(BringForward);
    }

    private void BuildCatalogView()
    {
        _activeWorkflow = null; _path.Clear(); _branch?.Hide(); _categories.Clear(); Row.Children.Clear();
        if (_workflows.Count == 0)
            Row.Children.Add(new TextBlock { Text = "Workflows 为空，点 + 打开目录添加文件夹", Foreground = Brushes.DimGray, Margin = new Thickness(12, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center });
        else
        {
            var width = GetUniformWidth(_workflows.Select(w => w.Name), 88, 180, 30);
            foreach (var workflow in _workflows)
            {
                var button = CreateTextButton(workflow.Name, width, "CatalogButtonStyle", workflow.Path);
                AutomationProperties.SetName(button, workflow.Name);
                button.Click += (_, _) =>
                {
                    if (_path.FirstOrDefault()?.Path == workflow.Path) { _path.Clear(); _branch?.Hide(); UpdateSelection(); return; }
                    _path.Clear(); _path.Add(workflow); RenderBranches();
                };
                _categories[workflow.Path] = button; Row.Children.Add(button);
            }
        }
        AddUtilityButtons(); FitExpandedWindow();
    }

    private void BuildWorkflowView()
    {
        if (_activeWorkflow is null)
        {
            BuildCatalogView();
            return;
        }

        Row.Children.Clear();

        var back = CreateUtilityButton(OneBarIcon.Back, "返回一级目录", () => BuildCatalogView());
        Row.Children.Add(back);

        var workflowWidth = GetUniformWidth(new[] { _activeWorkflow.Name }, 126, 180, 28);
        var workflowButton = CreateTextButton(_activeWorkflow.Name, workflowWidth, "WorkflowButtonStyle", "当前工作流");
        workflowButton.Click += (_, _) => BuildCatalogView();
        Row.Children.Add(workflowButton);

        Row.Children.Add(new TextBlock
        {
            Text = "➜",
            Foreground = new SolidColorBrush(Color.FromRgb(96, 102, 106)),
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(4, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        if (_activeWorkflow.Steps.Count == 0)
        {
            Row.Children.Add(new TextBlock
            {
                Text = "放入 .md / .txt 后点 ↻",
                Foreground = new SolidColorBrush(Color.FromRgb(96, 102, 106)),
                FontSize = 12,
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        else
        {
            var stepWidth = GetUniformWidth(_activeWorkflow.Steps.Select(s => s.Name), 145, 230, 32);
            for (var i = 0; i < _activeWorkflow.Steps.Count; i++)
            {
                var step = _activeWorkflow.Steps[i];
                var style = i % 2 == 0 ? "StepDarkButtonStyle" : "StepLightButtonStyle";
                var button = CreateTextButton(step.Name, stepWidth, style, "单击复制；按住拖动文件");
                AttachStepBehavior(button, step);
                Row.Children.Add(button);
            }
        }

        AddUtilityButtons();
        FitExpandedWindow();
    }

    private void AddUtilityButtons()
    {
        Row.Children.Add(new Border { Width = 10 });

        _pinButton = CreateUtilityButton(
            _isPinned ? OneBarIcon.LockClosed : OneBarIcon.LockOpen,
            "锁定 / 自动收回",
            () =>
            {
                _isPinned = !_isPinned;
                _collapseDelayTimer.Stop();
                UpdatePinIcon();
                if (!_isPinned) ScheduleCollapse();
            });
        Row.Children.Add(_pinButton);

        Row.Children.Add(CreateUtilityButton(OneBarIcon.Settings, "OneBar 设置", ShowSettings));
        Row.Children.Add(CreateUtilityButton(OneBarIcon.Plus, "打开 Workflows 文件夹", () => _shell.OpenWorkflowRoot(_settings.WorkflowRoot)));
        Row.Children.Add(CreateUtilityButton(OneBarIcon.Refresh, "重新读取工作流", () =>
        {
            ReloadWorkflows();
            BuildCatalogView();
        }));
        Row.Children.Add(CreateUtilityButton(OneBarIcon.Close, "关闭窗口；可在设置中改为退出软件", HandleCloseButton));
    }

    private void UpdatePinIcon()
    {
        if (_pinButton is not null)
            _pinButton.Content = IconFactory.Create(_isPinned ? OneBarIcon.LockClosed : OneBarIcon.LockOpen);
    }

    private Button CreateTextButton(string text, double width, string styleKey, string tooltip)
    {
        var label = new TextBlock
        {
            Text = text,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };

        return new Button
        {
            Content = label,
            Width = width,
            Style = (Style)FindResource(styleKey),
            ToolTip = tooltip
        };
    }

    private Button CreateUtilityButton(OneBarIcon icon, string tooltip, Action action)
    {
        var button = new Button
        {
            Content = IconFactory.Create(icon),
            Style = (Style)FindResource("UtilityButtonStyle"),
            ToolTip = tooltip
        };
        AutomationProperties.SetName(button, tooltip);
        button.Click += (_, _) => action();
        return button;
    }

    private void AttachStepBehavior(Button button, WorkflowStep step, Action? onCopied = null)
    {
        button.PreviewMouseLeftButtonDown += (_, _) =>
        {
            _mouseDownButton = button;
            _mouseDownPoint = Mouse.GetPosition(button);
            _suppressNextClick = false;
        };

        button.PreviewMouseMove += (_, _) =>
        {
            if (!ReferenceEquals(button, _mouseDownButton)) return;
            if (Mouse.LeftButton != MouseButtonState.Pressed) return;

            var current = Mouse.GetPosition(button);
            var dx = Math.Abs(current.X - _mouseDownPoint.X);
            var dy = Math.Abs(current.Y - _mouseDownPoint.Y);
            if (dx < DragThreshold && dy < DragThreshold) return;
            if (!File.Exists(step.Path)) return;

            _isDragging = true;
            _suppressNextClick = true;
            _collapseDelayTimer.Stop();
            _collapseAnimTimer.Stop();

            try
            {
                var data = new DataObject();
                data.SetData(DataFormats.FileDrop, new[] { step.Path });
                _branch?.Hide();
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
            }
            finally
            {
                _isDragging = false;
                _mouseDownButton = null;
                if (_isPinned && _path.Count > 0) RenderBranches();
                if (!_isPinned) ScheduleCollapse();
            }
        };

        button.PreviewMouseLeftButtonUp += (_, _) => _mouseDownButton = null;

        button.Click += (_, _) =>
        {
            if (_suppressNextClick)
            {
                _suppressNextClick = false;
                return;
            }

            try
            {
                if (!File.Exists(step.Path)) return;
                Clipboard.SetText(File.ReadAllText(step.Path));
                onCopied?.Invoke();
                ShowCopyFeedback(button, step.Name);
            }
            catch { }
        };
    }

    private static void ShowCopyFeedback(Button button, string original)
    {
        if (button.Content is not TextBlock label) return;
        label.Text = $"✓  {original}";

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            label.Text = original;
        };
        timer.Start();
    }

    private double GetUniformWidth(IEnumerable<string> labels, double min, double max, double padding)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var width = min;
        foreach (var text in labels)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                11,
                Brushes.Black,
                dpi);
            width = Math.Max(width, formatted.WidthIncludingTrailingWhitespace + padding);
        }
        return Math.Clamp(width, min, max);
    }

    private void FitExpandedWindow()
    {
        Dispatcher.BeginInvoke(() =>
        {
            var maxWidth = Math.Max(CollapsedWidth, SystemParameters.PrimaryScreenWidth - 30);
            ExpandedPanel.MaxWidth = maxWidth;
            ExpandedPanel.Measure(new Size(double.PositiveInfinity, ExpandedHeight));
            Width = Math.Min(maxWidth, Math.Max(CollapsedWidth, ExpandedPanel.DesiredSize.Width));
            Height = ExpandedPanel.Visibility == Visibility.Visible ? ExpandedHeight : CollapsedHeight;
            CenterWindow();
            if (_path.Count > 0 && ExpandedPanel.Visibility == Visibility.Visible) RenderBranches();
        }, DispatcherPriority.Loaded);
    }

    private void CenterWindow()
    {
        Left = Math.Max(0, (SystemParameters.PrimaryScreenWidth - Width) / 2);
        Top = 0;
    }

    private void ExpandBar()
    {
        _collapseDelayTimer.Stop();
        _collapseAnimTimer.Stop();
        _isCollapsing = false;
        PanelScale.ScaleY = 1;
        ExpandedPanel.Opacity = 1;
        CollapsedHandle.Visibility = Visibility.Collapsed;
        CollapsedGloss.Visibility = Visibility.Collapsed;
        ExpandedPanel.Visibility = Visibility.Visible;
        Height = ExpandedHeight;

        if (_path.Count > 0) FitExpandedWindow();
        else BuildCatalogView();
    }

    private void CollapseInstant()
    {
        _branch?.Hide();
        _collapseDelayTimer.Stop();
        _collapseAnimTimer.Stop();
        _isCollapsing = false;
        ExpandedPanel.Visibility = Visibility.Collapsed;
        CollapsedHandle.Visibility = Visibility.Visible;
        CollapsedGloss.Visibility = Visibility.Visible;
        PanelScale.ScaleY = 1;
        ExpandedPanel.Opacity = 1;
        Width = CollapsedWidth;
        Height = CollapsedHeight;
        CenterWindow();
    }

    internal void ScheduleCollapse()
    {
        if (_isPinned || _isDragging || _settingsOpen) return;
        _collapseDelayTimer.Stop();
        _collapseDelayTimer.Interval = TimeSpan.FromMilliseconds(_settings.CollapseDelayMs);
        _collapseDelayTimer.Start();
    }

    private void StartCollapseAnimation(bool force = false)
    {
        if (_isPinned || _isDragging || _settingsOpen || _isCollapsing || (!force && (IsMouseOver || _branch?.IsMouseOver == true)) || ExpandedPanel.Visibility != Visibility.Visible) return;
        _collapseLevels = _branch?.IsVisible == true ? _branch.LevelCount : 0;
        _isCollapsing = true;
        _collapseStopwatch.Restart();
        _collapseAnimTimer.Start();
    }

    private void CollapseAnimTimerOnTick(object? sender, EventArgs e)
    {
        if (_isPinned || _isDragging || _settingsOpen) { ExpandBar(); return; }
        var total = Math.Max(200, _settings.CollapseAnimMs);
        var elapsed = _collapseStopwatch.Elapsed.TotalMilliseconds;
        if (elapsed >= total)
        {
            _branch?.ApplyRetraction(_collapseLevels);
            CollapseInstant();
            return;
        }
        var stages = elapsed / total * (_collapseLevels + 1);
        if (_collapseLevels > 0) _branch?.ApplyRetraction(stages);
        var navigationProgress = Math.Clamp(stages - _collapseLevels, 0, 1);
        var scale = Math.Pow(1 - navigationProgress, 3);
        PanelScale.ScaleY = scale;
        ExpandedPanel.Opacity = scale;
        Height = CollapsedHeight + ((ExpandedHeight - CollapsedHeight) * scale);
    }

    private void HandleCloseButton()
    {
        if (string.Equals(_settings.CloseButtonAction, "exit", StringComparison.OrdinalIgnoreCase))
            ExitOneBar();
        else
        {
            _isPinned = false;
            UpdatePinIcon();
            _collapseDelayTimer.Stop();
            CollapseInstant();
        }
    }

    private void ExitOneBar()
    {
        _allowExit = true;
        Dispose();
        Application.Current.Shutdown();
    }


    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _isPinned = false;
            UpdatePinIcon();
            _collapseDelayTimer.Stop();
            CollapseInstant();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ReloadWorkflows();
            BuildCatalogView();
            e.Handled = true;
        }
    }

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowExit) return;
        e.Cancel = true;
        HandleCloseButton();
    }

    private void CollapsedHandle_OnMouseEnter(object sender, MouseEventArgs e) => ExpandBar();

    private void ExpandedPanel_OnMouseEnter(object sender, MouseEventArgs e)
    {
        _collapseDelayTimer.Stop();
        if (_isCollapsing) ExpandBar();
    }

    private void ExpandedPanel_OnMouseLeave(object sender, MouseEventArgs e) => ScheduleCollapse();

    private void Scroller_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Scroller.ScrollToHorizontalOffset(Scroller.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void UpdateSelection()
    {
        foreach (var (path, button) in _categories)
            button.Style = (Style)FindResource(_path.FirstOrDefault()?.Path == path ? "WorkflowButtonStyle" : "CatalogButtonStyle");
    }
    private void RenderBranches()
    {
        if (_path.Count == 0 || !_categories.TryGetValue(_path[0].Path, out var anchor)) return;
        UpdateSelection(); UpdateLayout(); _branch ??= new BranchWindow(this);
        var dpi = VisualTreeHelper.GetDpi(this);
        var point = anchor.PointToScreen(new Point(anchor.ActualWidth / 2, anchor.ActualHeight));
        _branch.Render(_path, point.X / dpi.DpiScaleX, Top + ExpandedHeight);
        if (!_branch.IsVisible) _branch.Show();
    }
    internal Button Capsule(string label, double width, string style, string tip)
    {
        var button = CreateTextButton(label, width, style, tip);
        if (button.Content is TextBlock text) text.MaxWidth = width - 24;
        button.Margin = new Thickness(0); AutomationProperties.SetName(button, label);
        button.MouseEnter += (_, _) => CancelCollapse();
        return button;
    }
    internal void SelectFolder(int level, WorkflowDefinition node)
    {
        CancelCollapse();
        if (_path.Count > level + 1 && _path[level + 1].Path == node.Path)
            _path.RemoveRange(level + 1, _path.Count - level - 1);
        else
        {
            if (_path.Count > level + 1) _path.RemoveRange(level + 1, _path.Count - level - 1);
            _path.Add(node);
        }
        RenderBranches();
    }
    internal void AttachFile(Button button, WorkflowStep step, Action? onCopied = null) => AttachStepBehavior(button, step, onCopied);
    internal void CancelCollapse()
    {
        _collapseDelayTimer.Stop();
        if (_isCollapsing) ExpandBar();
    }
    internal static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
