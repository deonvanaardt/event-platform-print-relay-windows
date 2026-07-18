namespace EventPlatform.PrintRelay.App.Tray;

/// <summary>
/// Clipboard access from NotifyIcon menu callbacks. Marshals with <see cref="Control.BeginInvoke"/>
/// (not <see cref="Control.Invoke"/>, which can run inline when <see cref="Control.InvokeRequired"/>
/// is false on a non-STA menu thread).
/// </summary>
internal static class StaClipboard
{
    private const int RetryTimes = 10;
    private const int RetryDelayMs = 100;

    public static void SetText(string text, Control uiThread, int uiThreadId)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(uiThread);

        if (uiThread.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(uiThread));
        }

        UiThreadSync.Run(uiThread, uiThreadId, () => SetTextWithRetry(text));
    }

    private static void SetTextWithRetry(string text)
    {
        Clipboard.SetDataObject(text, copy: true, RetryTimes, RetryDelayMs);
    }
}

/// <summary>
/// Posts work to a control's UI thread. Uses BeginInvoke so the delegate always runs on the
/// thread that created the control handle, even when InvokeRequired is false on the caller.
/// </summary>
internal static class UiThreadSync
{
    public static void Run(Control control, int uiThreadId, Action action)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(action);

        if (control.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(control));
        }

        if (Environment.CurrentManagedThreadId == uiThreadId)
        {
            action();
            return;
        }

        var done = new ManualResetEventSlim(false);
        Exception? error = null;

        control.BeginInvoke(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        done.Wait();

        if (error is not null)
        {
            throw error;
        }
    }

    public static void Post(Control control, Action action)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(action);

        if (control.IsDisposed)
        {
            return;
        }

        control.BeginInvoke(action);
    }
}
