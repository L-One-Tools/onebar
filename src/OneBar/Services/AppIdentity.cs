using System.Runtime.InteropServices;

namespace OneBar.Services;

internal static class AppIdentity
{
    public const string AppUserModelId = "L-One.OneBar";

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);

    public static void Apply()
    {
        try { _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId); }
        catch { }
    }
}
