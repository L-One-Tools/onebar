using System.Text.Json;
using OneBar.Models;

namespace OneBar.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneBar");

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public AppSettings Load()
    {
        AppSettings settings = new();

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            settings = new AppSettings();
        }

        Normalize(settings);
        EnsureWorkflowRoot(settings.WorkflowRoot);
        SeedOrMigrateWorkflowRoot(settings.WorkflowRoot);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);
        Directory.CreateDirectory(SettingsDirectory);
        EnsureWorkflowRoot(settings.WorkflowRoot);

        var temp = SettingsPath + ".tmp";
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(temp, json);
        File.Copy(temp, SettingsPath, true);
        File.Delete(temp);
    }

    public void ApplyInstallerConfiguration(
        AppSettings settings,
        bool autoStart,
        bool desktopShortcut,
        bool startMenuShortcut,
        bool showTaskbar,
        bool showTray,
        string workflowRoot)
    {
        var oldRoot = settings.WorkflowRoot;
        var newRoot = Environment.ExpandEnvironmentVariables(workflowRoot.Trim());
        if (string.IsNullOrWhiteSpace(newRoot))
            newRoot = DefaultWorkflowRoot();

        EnsureWorkflowRoot(newRoot);
        if (!PathsEqual(oldRoot, newRoot))
            CopyDirectoryIfTargetEmpty(oldRoot, newRoot);

        settings.AutoStart = autoStart;
        settings.DesktopShortcut = desktopShortcut;
        settings.StartMenuShortcut = startMenuShortcut;
        settings.ShowTaskbarIcon = showTaskbar;
        settings.ShowTrayIcon = showTray;
        settings.WorkflowRoot = newRoot;
        Save(settings);
    }

    public void ChangeWorkflowRoot(AppSettings settings, string newRoot)
    {
        newRoot = Environment.ExpandEnvironmentVariables(newRoot.Trim());
        if (string.IsNullOrWhiteSpace(newRoot))
            return;

        EnsureWorkflowRoot(newRoot);
        if (!PathsEqual(settings.WorkflowRoot, newRoot))
            CopyDirectoryIfTargetEmpty(settings.WorkflowRoot, newRoot);

        settings.WorkflowRoot = newRoot;
        Save(settings);
    }

    private void Normalize(AppSettings settings)
    {
        settings.CloseButtonAction = string.Equals(settings.CloseButtonAction, "exit", StringComparison.OrdinalIgnoreCase)
            ? "exit"
            : "hide";
        settings.CollapseDelayMs = Math.Clamp(settings.CollapseDelayMs, 0, 10000);
        settings.CollapseAnimMs = Math.Clamp(settings.CollapseAnimMs, 200, 5000);

        if (string.IsNullOrWhiteSpace(settings.WorkflowRoot))
            settings.WorkflowRoot = DefaultWorkflowRoot();
        else
            settings.WorkflowRoot = Environment.ExpandEnvironmentVariables(settings.WorkflowRoot);
    }

    private static string DefaultWorkflowRoot() => Path.Combine(AppContext.BaseDirectory, "Workflows");

    private static void EnsureWorkflowRoot(string path)
    {
        try { Directory.CreateDirectory(path); }
        catch { }
    }

    private void SeedOrMigrateWorkflowRoot(string target)
    {
        if (!Directory.Exists(target)) return;
        if (Directory.EnumerateFileSystemEntries(target).Any()) return;

        var candidates = new List<string>();

        // Current prototype settings may point at an older workflow root.
        try
        {
            if (File.Exists(SettingsPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
                if (doc.RootElement.TryGetProperty("workflowRoot", out var wr) && wr.ValueKind == JsonValueKind.String)
                {
                    var value = wr.GetString();
                    if (!string.IsNullOrWhiteSpace(value)) candidates.Add(Environment.ExpandEnvironmentVariables(value));
                }
            }
        }
        catch { }

        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OneBar", "Workflows"));
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuckBar", "Workflows"));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "Defaults", "Workflows"));

        foreach (var source in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(source) || PathsEqual(source, target)) continue;
            if (!Directory.EnumerateFileSystemEntries(source).Any()) continue;
            CopyDirectory(source, target);
            if (Directory.EnumerateFileSystemEntries(target).Any()) break;
        }
    }

    private static void CopyDirectoryIfTargetEmpty(string source, string target)
    {
        try
        {
            if (!Directory.Exists(target)) Directory.CreateDirectory(target);
            if (Directory.EnumerateFileSystemEntries(target).Any()) return;
            if (!Directory.Exists(source)) return;
            CopyDirectory(source, target);
        }
        catch { }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(target, relative));
        }
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, false);
        }
    }

    private static bool PathsEqual(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        try
        {
            return string.Equals(
                Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
    }
}
