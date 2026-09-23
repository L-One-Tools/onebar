using System.Windows;
using OneBar.Models;
using OneBar.Services;
using OneBar.UI;

namespace OneBar;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private SettingsService? _settingsService;
    private ShellIntegrationService? _shellIntegration;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppIdentity.Apply();

        _settingsService = new SettingsService();
        _shellIntegration = new ShellIntegrationService();

        var settings = _settingsService.Load();

        if (InstallerArgumentParser.TryParse(e.Args, out var installerConfig) && installerConfig is not null)
        {
            _settingsService.ApplyInstallerConfiguration(
                settings,
                installerConfig.AutoStart,
                installerConfig.DesktopShortcut,
                installerConfig.StartMenuShortcut,
                installerConfig.ShowTaskbar,
                installerConfig.ShowTray,
                installerConfig.WorkflowRoot);
            _shellIntegration.Apply(settings);
            Shutdown(0);
            return;
        }

        var wantsSettings = e.Args.Any(x => string.Equals(x, "--settings", StringComparison.OrdinalIgnoreCase));

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            _singleInstance.SignalExisting(wantsSettings);
            Shutdown(0);
            return;
        }

        _mainWindow = new MainWindow(_settingsService, _shellIntegration, settings);
        MainWindow = _mainWindow;
        _mainWindow.Show();

        _singleInstance.StartListening(
            () => Dispatcher.Invoke(() => _mainWindow.BringForward()),
            () => Dispatcher.Invoke(() =>
            {
                _mainWindow.BringForward();
                _mainWindow.ShowSettings();
            }));

        if (wantsSettings)
            _mainWindow.ShowSettings();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _mainWindow?.Dispose(); } catch { }
        try { _singleInstance?.Dispose(); } catch { }
        base.OnExit(e);
    }
}
