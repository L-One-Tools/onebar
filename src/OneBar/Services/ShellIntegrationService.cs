using System.Diagnostics;
using Microsoft.Win32;
using OneBar.Models;

namespace OneBar.Services;

public sealed class ShellIntegrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "OneBar";

    public void Apply(AppSettings settings)
    {
        SetAutoStart(settings.AutoStart);
        SetShortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), settings.DesktopShortcut);
        SetShortcut(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), settings.StartMenuShortcut, "Programs", "OneBar");
    }

    public void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enabled)
            {
                key?.SetValue(RunValueName, $"\"{ExecutablePath()}\"");
            }
            else
            {
                key?.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
        catch { }
    }

    public void OpenWorkflowRoot(string workflowRoot)
    {
        try
        {
            Directory.CreateDirectory(workflowRoot);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{workflowRoot}\"") { UseShellExecute = true });
        }
        catch { }
    }

    public void OpenInstallRoot()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{AppContext.BaseDirectory}\"") { UseShellExecute = true });
        }
        catch { }
    }

    private static void SetShortcut(string baseFolder, bool enabled, params string[] subFolders)
    {
        try
        {
            var folder = subFolders.Aggregate(baseFolder, Path.Combine);
            Directory.CreateDirectory(folder);
            var shortcutPath = Path.Combine(folder, "OneBar.lnk");

            if (!enabled)
            {
                if (File.Exists(shortcutPath)) File.Delete(shortcutPath);
                return;
            }

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = ExecutablePath();
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.IconLocation = $"{ExecutablePath()},0";
            shortcut.Description = "OneBar - Top-edge workflow launcher";
            shortcut.Save();
        }
        catch { }
    }

    private static string ExecutablePath()
        => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OneBar.exe");
}
