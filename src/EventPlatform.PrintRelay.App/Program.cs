using EventPlatform.PrintRelay.App.Setup;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.App;

internal static class Program
{
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

        if (settings.IsComplete())
        {
            return 0;
        }

        using var http = new HttpClient();
        Application.Run(new SetupWizardForm(http, settingsPath));
        return 0;
    }
}
