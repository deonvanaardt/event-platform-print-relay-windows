namespace EventPlatform.PrintRelay.App.Tray;

/// <summary>
/// Clipboard access from NotifyIcon menu callbacks, which may run on a non-STA thread
/// even when <see cref="Control.InvokeRequired"/> is false on the sync form.
/// </summary>
internal static class StaClipboard
{
    public static void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            Clipboard.SetText(text);
            return;
        }

        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true,
            Name = "StaClipboard",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw error;
        }
    }
}
