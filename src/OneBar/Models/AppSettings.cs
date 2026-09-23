namespace OneBar.Models;

public sealed class AppSettings
{
    public bool AutoStart { get; set; } = true;
    public bool DesktopShortcut { get; set; } = true;
    public bool StartMenuShortcut { get; set; } = true;
    public bool ShowTaskbarIcon { get; set; } = true;
    public bool ShowTrayIcon { get; set; } = true;
    public string CloseButtonAction { get; set; } = "hide";
    public int CollapseDelayMs { get; set; } = 1000;
    public int CollapseAnimMs { get; set; } = 1000;
    public string WorkflowRoot { get; set; } = string.Empty;
}
