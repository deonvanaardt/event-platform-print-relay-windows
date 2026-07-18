using System.Diagnostics;
using EventPlatform.PrintRelay.App.Printing;
using EventPlatform.PrintRelay.App.Setup;
using EventPlatform.PrintRelay.App.Tray;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (IsVersionRequest(args))
        {
            WriteVersionToConsole();
            return 0;
        }

        ApplicationConfiguration.Initialize();
        PdfiumNativeBootstrap.EnsureLoaded();

        if (IsAboutRequest(args))
        {
            ShowAboutDialog();
            return 0;
        }

        RelayStartupLog.Write(
            $"Process started — version {RelayAppInfo.AppVersion}, tray build.");

        try
        {
            return RunAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            RelayStartupLog.Write($"Fatal error: {ex}");
            MessageBox.Show(
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static bool IsVersionRequest(string[] args) =>
        args.Any(arg =>
            string.Equals(arg, "--version", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "/version", StringComparison.OrdinalIgnoreCase));

    private static bool IsAboutRequest(string[] args) =>
        args.Any(arg =>
            string.Equals(arg, "--about", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "/about", StringComparison.OrdinalIgnoreCase));

    private static string BuildAboutText()
    {
        var buildInfoPath = Path.Combine(AppContext.BaseDirectory, "build-info.txt");
        var buildInfo = File.Exists(buildInfoPath)
            ? File.ReadAllText(buildInfoPath).Trim()
            : "build-info.txt not found.";

        return $"Print Relay {RelayAppInfo.AppVersion}{Environment.NewLine}{buildInfo}";
    }

    private static void WriteVersionToConsole()
    {
        var text = BuildAboutText();
        Console.WriteLine(text);
    }

    private static void ShowAboutDialog()
    {
        MessageBox.Show(
            BuildAboutText(),
            "Print Relay",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static async Task<int> RunAsync()
    {
        var settingsPath = RelaySettingsStore.GetDefaultSettingsPath();
        var settings = await RelaySettingsStore.LoadAsync(settingsPath).ConfigureAwait(true);

        if (!settings.IsComplete())
        {
            RelayStartupLog.Write("Setup wizard required — settings incomplete.");
            using var http = new HttpClient();
            Application.Run(new SetupWizardForm(http, settingsPath));
            settings = await RelaySettingsStore.LoadAsync(settingsPath).ConfigureAwait(true);
        }

        if (settings is null || !settings.IsComplete())
        {
            RelayStartupLog.Write("Exiting — setup was not completed.");
            return 0;
        }

        RelayStartupLog.Write("Starting tray application context.");
        var (restart, restartReason) = RunRelay(settings, settingsPath);

        if (!restart)
        {
            return 0;
        }

        RelayStartupLog.Write($"Restart requested: {restartReason}.");

        if (restartReason == RelayRestartReason.ResetSetup)
        {
            await RelaySettingsStore.DeleteAsync(settingsPath).ConfigureAwait(true);
            RelayStartupLog.Write("Settings cleared for setup reset.");
        }

        RelayStartupLog.Write("Spawning new process for restart.");
        RestartProcess();
        return 0;
    }

    private static void RestartProcess()
    {
        var exePath = Environment.ProcessPath ?? Application.ExecutablePath;

        if (string.IsNullOrEmpty(exePath))
        {
            throw new InvalidOperationException("Could not determine executable path for restart.");
        }

        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory,
        });

        Environment.Exit(0);
    }

    private static (bool Restart, RelayRestartReason Reason) RunRelay(
        RelaySettings settings,
        string settingsPath)
    {
        using var tray = new TrayApplicationContext(settings, settingsPath);
        Application.Run(tray);
        return (tray.RestartRequested, tray.RestartReason);
    }
}
