namespace OneBar.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\OneBar_SingleInstance";
    private const string ShowEventName = @"Local\OneBar_ShowEvent";
    private const string SettingsEventName = @"Local\OneBar_SettingsEvent";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private EventWaitHandle? _settingsEvent;
    private CancellationTokenSource? _cts;

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew) return false;

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _settingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, SettingsEventName);
        return true;
    }

    public void SignalExisting(bool showSettings)
    {
        var eventName = showSettings ? SettingsEventName : ShowEventName;
        for (var i = 0; i < 6; i++)
        {
            try
            {
                using var evt = EventWaitHandle.OpenExisting(eventName);
                evt.Set();
                return;
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    public void StartListening(Action showCallback, Action settingsCallback)
    {
        if (_showEvent is null || _settingsEvent is null) return;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var handles = new WaitHandle[] { _showEvent, _settingsEvent };

        _ = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var index = WaitHandle.WaitAny(handles, 500);
                    if (token.IsCancellationRequested) break;
                    if (index == 0) showCallback();
                    else if (index == 1) settingsCallback();
                }
                catch { break; }
            }
        }, token);
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _showEvent?.Dispose(); } catch { }
        try { _settingsEvent?.Dispose(); } catch { }
        try { _mutex?.ReleaseMutex(); } catch { }
        try { _mutex?.Dispose(); } catch { }
        _cts?.Dispose();
    }
}
