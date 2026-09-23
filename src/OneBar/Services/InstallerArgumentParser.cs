namespace OneBar.Services;

public sealed class InstallerConfiguration
{
    public bool AutoStart { get; init; }
    public bool DesktopShortcut { get; init; }
    public bool StartMenuShortcut { get; init; }
    public bool ShowTaskbar { get; init; }
    public bool ShowTray { get; init; }
    public required string WorkflowRoot { get; init; }
}

public static class InstallerArgumentParser
{
    public static bool TryParse(string[] args, out InstallerConfiguration? configuration)
    {
        configuration = null;
        if (!args.Any(x => string.Equals(x, "--installer-config", StringComparison.OrdinalIgnoreCase)))
            return false;

        configuration = new InstallerConfiguration
        {
            AutoStart = GetBool(args, "--autostart", true),
            DesktopShortcut = GetBool(args, "--desktop", true),
            StartMenuShortcut = GetBool(args, "--startmenu", true),
            ShowTaskbar = GetBool(args, "--taskbar", true),
            ShowTray = GetBool(args, "--tray", true),
            WorkflowRoot = GetString(args, "--workflowRoot") ?? Path.Combine(AppContext.BaseDirectory, "Workflows")
        };
        return true;
    }

    private static bool GetBool(IEnumerable<string> args, string key, bool fallback)
    {
        var value = GetString(args, key);
        if (value is null) return fallback;
        return value is "1" or "true" or "yes" or "on";
    }

    private static string? GetString(IEnumerable<string> args, string key)
    {
        var prefix = key + "=";
        var match = args.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return match is null ? null : match[prefix.Length..].Trim('"');
    }
}
