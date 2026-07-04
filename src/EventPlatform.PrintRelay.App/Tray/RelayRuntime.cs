using System.Reflection;
using EventPlatform.PrintRelay.App.Printing;
using EventPlatform.PrintRelay.Core.Diagnostics;
using EventPlatform.PrintRelay.Core.Logging;
using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Settings;
using Microsoft.Web.WebView2.Core;

namespace EventPlatform.PrintRelay.App.Tray;

internal static class RelayAppInfo
{
    public static string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    public static string? TryGetWebView2Version()
    {
        try
        {
            return CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch
        {
            return null;
        }
    }
}

internal enum RelayTrayIconState
{
    Connected,
    Reconnecting,
    Error,
}

internal sealed class RelayRuntime : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly HttpClient _http;
    private readonly RelayFileLogger _fileLogger;
    private readonly Task? _pollTask;
    private bool _disposed;

    public RelayRuntime(RelaySettings settings, string settingsPath)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        SettingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));

        SessionState = new RelaySessionState();
        _fileLogger = new RelayFileLogger(RelayFileLogger.GetDefaultLogPath());
        ActivitySink = new CompositeRelayActivitySink(SessionState, _fileLogger);

        _http = new HttpClient();
        Api = new Core.Api.PrintRelayApiClient(_http, settings.ApiUrl, settings.Secret);
        Printer = new WebView2SilentPrinter();

        var printerInstalled = InstalledPrinters.Exists(settings.PrinterName);
        SessionState.SetPrinterInstalled(printerInstalled);

        if (printerInstalled)
        {
            var processor = new Polling.BadgeHtmlPrintJobProcessor(settings.PrinterName, Printer);
            var loop = new PrintRelayPollLoop(
                Api,
                processor,
                new PollBackoff(),
                activitySink: ActivitySink);

            _pollTask = loop.RunAsync(_cancellation.Token);
            _fileLogger.LogInfo("Relay started.");
        }
        else
        {
            ActivitySink.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.ConnectionStateChanged,
                Message = "Printer not found — open Settings to select a printer.",
            });

            _fileLogger.LogInfo("Relay started without polling — configured printer not found.");
        }
    }

    public RelaySettings Settings { get; private set; }

    public string SettingsPath { get; }

    public RelaySessionState SessionState { get; }

    public IRelayActivitySink ActivitySink { get; }

    public Core.Api.PrintRelayApiClient Api { get; }

    public WebView2SilentPrinter Printer { get; }

    public RelayTrayIconState GetTrayIconState()
    {
        var snapshot = SessionState.GetSnapshot(Settings);

        if (!snapshot.PrinterInstalled)
        {
            return RelayTrayIconState.Error;
        }

        return snapshot.ConnectionState switch
        {
            PrintRelayPollConnectionState.BackingOff => RelayTrayIconState.Reconnecting,
            PrintRelayPollConnectionState.AuthError => RelayTrayIconState.Error,
            _ => RelayTrayIconState.Connected,
        };
    }

    public string GetTrayTooltip()
    {
        return GetTrayIconState() switch
        {
            RelayTrayIconState.Reconnecting => "Print Relay — Reconnecting…",
            RelayTrayIconState.Error when !SessionState.GetSnapshot(Settings).PrinterInstalled =>
                "Print Relay — Error (printer not found)",
            RelayTrayIconState.Error => "Print Relay — Error (click for details)",
            _ => "Print Relay — Connected",
        };
    }

    public async Task UpdatePrinterAsync(string printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        var updated = Settings with { PrinterName = printerName };
        await RelaySettingsStore.SaveAsync(updated, SettingsPath).ConfigureAwait(true);
        Settings = updated;

        var installed = InstalledPrinters.Exists(printerName);
        SessionState.SetPrinterInstalled(installed);

        ActivitySink.Record(new RelayActivityEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = RelayActivityKind.ConnectionStateChanged,
            Message = installed
                ? $"Printer changed to {printerName}."
                : $"Printer {printerName} was saved but is not currently installed.",
        });
    }

    public async Task TestConnectionAsync()
    {
        await RelayConnectionTester
            .TestConnectionAsync(Api, ActivitySink, _cancellation.Token)
            .ConfigureAwait(true);
    }

    public async Task PrintTestBadgeAsync()
    {
        var html = TestBadgeHtmlLoader.Load(Settings.DeskName);
        await Printer.PrintHtmlAsync(html, Settings.PrinterName, _cancellation.Token)
            .ConfigureAwait(true);

        ActivitySink.Record(new RelayActivityEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = RelayActivityKind.PrintCompleted,
            Message = "Test badge sent to printer.",
        });
    }

    public string BuildDiagnosticsJson()
    {
        var snapshot = SessionState.GetSnapshot(Settings);
        var diagnostics = RelayDiagnosticsBuilder.BuildFromSettings(
            Settings,
            snapshot,
            RelayAppInfo.AppVersion,
            RelayAppInfo.TryGetWebView2Version());

        return RelayDiagnosticsBuilder.ToJson(diagnostics);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ActivitySink.Record(new RelayActivityEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = RelayActivityKind.RelayStopped,
            Message = "Relay stopped.",
        });

        _fileLogger.LogStopped();
        _cancellation.Cancel();

        if (_pollTask is not null)
        {
            try
            {
                _pollTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(
                inner => inner is OperationCanceledException))
            {
            }
        }

        Printer.Dispose();
        _http.Dispose();
        _fileLogger.Dispose();
        _cancellation.Dispose();
    }
}

internal static class TestBadgeHtmlLoader
{
    public static string Load(string deskName)
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "test-badge-cr80.html");

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException("Test badge fixture was not found.", fixturePath);
        }

        var html = File.ReadAllText(fixturePath);
        return html.Replace(
            "id=\"desk-name\">Desk",
            $"id=\"desk-name\">{System.Net.WebUtility.HtmlEncode(deskName)}",
            StringComparison.Ordinal);
    }
}
