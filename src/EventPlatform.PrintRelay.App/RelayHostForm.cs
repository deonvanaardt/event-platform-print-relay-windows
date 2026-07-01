namespace EventPlatform.PrintRelay.App;

internal sealed class RelayHostForm : Form
{
    private readonly CancellationTokenSource _cancellation;

    public RelayHostForm(CancellationTokenSource cancellation)
    {
        _cancellation = cancellation ?? throw new ArgumentNullException(nameof(cancellation));

        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
        Width = 1;
        Height = 1;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cancellation.Cancel();
        base.OnFormClosing(e);
    }
}
