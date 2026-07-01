using EventPlatform.PrintRelay.App.Polling;
using EventPlatform.PrintRelay.App.Printing;
using EventPlatform.PrintRelay.App.Setup;
using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.App;

internal static class Program
{
    private const string WebView2InstallUrl =
        "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            return RunAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static async Task<int> RunAsync()
    {
        var settingsPath = RelaySettingsStore.GetDefaultSettingsPath();
        var settings = await RelaySettingsStore.LoadAsync(settingsPath).ConfigureAwait(true);

        if (!settings.IsComplete())
        {
            using var http = new HttpClient();
            Application.Run(new SetupWizardForm(http, settingsPath));
            settings = await RelaySettingsStore.LoadAsync(settingsPath).ConfigureAwait(true);
        }

        if (!settings.IsComplete())
        {
            return 0;
        }

        await RunRelayAsync(settings).ConfigureAwait(true);
        return 0;
    }

    private static async Task RunRelayAsync(RelaySettings settings)
    {
        using var printer = TryCreatePrinter();
        if (printer is null)
        {
            return;
        }

        using var http = new HttpClient();
        using var cancellation = new CancellationTokenSource();

        var api = new PrintRelayApiClient(http, settings.ApiUrl, settings.Secret);
        var processor = new BadgeHtmlPrintJobProcessor(settings.PrinterName, printer);
        var loop = new PrintRelayPollLoop(api, processor, new PollBackoff());

        var pollTask = loop.RunAsync(cancellation.Token);

        Application.Run(new RelayHostForm(cancellation));

        try
        {
            await pollTask.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static WebView2SilentPrinter? TryCreatePrinter()
    {
        try
        {
            return new WebView2SilentPrinter();
        }
        catch (InvalidOperationException)
        {
            var result = MessageBox.Show(
                "Print Relay needs the Microsoft Edge WebView2 Runtime.\n\nOpen the download page now?",
                "Print Relay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(WebView2InstallUrl)
                        {
                            UseShellExecute = true,
                        });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Print Relay",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return null;
        }
    }
}
