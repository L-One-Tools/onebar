using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using OneBar.Models;
using OneBar.Services;
using WinForms = System.Windows.Forms;

namespace OneBar.UI;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly ShellIntegrationService _shell;
    private readonly AppSettings _working;

    public AppSettings? UpdatedSettings { get; private set; }

    public SettingsWindow(SettingsService settingsService, ShellIntegrationService shell, AppSettings current)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _shell = shell;
        _working = Clone(current);

        AutoStartCheck.IsChecked = _working.AutoStart;
        DesktopCheck.IsChecked = _working.DesktopShortcut;
        StartMenuCheck.IsChecked = _working.StartMenuShortcut;
        TaskbarCheck.IsChecked = _working.ShowTaskbarIcon;
        TrayCheck.IsChecked = _working.ShowTrayIcon;
        DelayBox.Text = _working.CollapseDelayMs.ToString();
        AnimBox.Text = _working.CollapseAnimMs.ToString();
        WorkflowRootBox.Text = _working.WorkflowRoot;

        CloseActionCombo.SelectedIndex = string.Equals(_working.CloseButtonAction, "exit", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private void BrowseWorkflow_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "选择 OneBar 工作流目录",
            SelectedPath = WorkflowRootBox.Text
        };
        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            WorkflowRootBox.Text = dialog.SelectedPath;
    }

    private void OpenWorkflow_OnClick(object sender, RoutedEventArgs e)
    {
        var path = WorkflowRootBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path)) path = _working.WorkflowRoot;
        _shell.OpenWorkflowRoot(path);
    }

    private void OpenInstall_OnClick(object sender, RoutedEventArgs e) => _shell.OpenInstallRoot();

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DelayBox.Text.Trim(), out var delay) || delay < 0 || delay > 10000)
        {
            MessageBox.Show(this, "停留时间请输入 0–10000 毫秒。", "OneBar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(AnimBox.Text.Trim(), out var anim) || anim < 200 || anim > 5000)
        {
            MessageBox.Show(this, "回收动画请输入 200–5000 毫秒。", "OneBar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var workflowRoot = WorkflowRootBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(workflowRoot))
        {
            MessageBox.Show(this, "请选择工作流目录。", "OneBar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _working.AutoStart = AutoStartCheck.IsChecked == true;
        _working.DesktopShortcut = DesktopCheck.IsChecked == true;
        _working.StartMenuShortcut = StartMenuCheck.IsChecked == true;
        _working.ShowTaskbarIcon = TaskbarCheck.IsChecked == true;
        _working.ShowTrayIcon = TrayCheck.IsChecked == true;
        _working.CloseButtonAction = ((ComboBoxItem)CloseActionCombo.SelectedItem).Tag?.ToString() == "exit" ? "exit" : "hide";
        _working.CollapseDelayMs = delay;
        _working.CollapseAnimMs = anim;

        _settingsService.ChangeWorkflowRoot(_working, workflowRoot);
        _settingsService.Save(_working);
        _shell.Apply(_working);

        UpdatedSettings = _working;
        DialogResult = true;
        Close();
    }

    private static AppSettings Clone(AppSettings source) => new()
    {
        AutoStart = source.AutoStart,
        DesktopShortcut = source.DesktopShortcut,
        StartMenuShortcut = source.StartMenuShortcut,
        ShowTaskbarIcon = source.ShowTaskbarIcon,
        ShowTrayIcon = source.ShowTrayIcon,
        CloseButtonAction = source.CloseButtonAction,
        CollapseDelayMs = source.CollapseDelayMs,
        CollapseAnimMs = source.CollapseAnimMs,
        WorkflowRoot = source.WorkflowRoot
    };
}
