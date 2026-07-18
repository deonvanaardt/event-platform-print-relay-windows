namespace EventPlatform.PrintRelay.App.Tray;

/// <summary>
/// Clipboard access from NotifyIcon menu callbacks. Always marshals to a control on the
/// main UI thread (which runs the WinForms message pump) and retries transient OLE failures.
/// </summary>
internal static class StaClipboard
{
    private const int RetryTimes = 10;
    private const int RetryDelayMs = 100;

    public static void SetText(string text, Control uiThread)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(uiThread);

        if (uiThread.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(uiThread));
        }

        if (!uiThread.IsHandleCreated)
        {
            uiThread.CreateControl();
        }

        uiThread.Invoke(() => SetTextWithRetry(text));
    }

    private static void SetTextWithRetry(string text)
    {
        Clipboard.SetDataObject(text, copy: true, RetryTimes, RetryDelayMs);
    }
}
