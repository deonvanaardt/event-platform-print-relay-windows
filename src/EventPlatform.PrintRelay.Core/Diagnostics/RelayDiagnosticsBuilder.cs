using System.Text.Json;
using System.Text.Json.Serialization;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.Core.Diagnostics;

public sealed record RelayDiagnosticsSnapshot
{
    [JsonPropertyName("app_version")]
    public required string AppVersion { get; init; }

    [JsonPropertyName("desk_name")]
    public required string DeskName { get; init; }

    [JsonPropertyName("api_hostname")]
    public required string ApiHostname { get; init; }

    [JsonPropertyName("printer_name")]
    public required string PrinterName { get; init; }

    [JsonPropertyName("connection_state")]
    public required string ConnectionState { get; init; }

    [JsonPropertyName("last_successful_poll_utc")]
    public DateTimeOffset? LastSuccessfulPollUtc { get; init; }

    [JsonPropertyName("last_poll_latency_ms")]
    public int? LastPollLatencyMs { get; init; }

    [JsonPropertyName("last_pending_job_count")]
    public int LastPendingJobCount { get; init; }

    [JsonPropertyName("jobs_received_this_session")]
    public int JobsReceivedThisSession { get; init; }

    [JsonPropertyName("jobs_printed_this_session")]
    public int JobsPrintedThisSession { get; init; }

    [JsonPropertyName("jobs_failed_this_session")]
    public int JobsFailedThisSession { get; init; }

    [JsonPropertyName("last_job_id")]
    public string? LastJobId { get; init; }

    [JsonPropertyName("last_job_outcome")]
    public string? LastJobOutcome { get; init; }

    [JsonPropertyName("last_error_message")]
    public string? LastErrorMessage { get; init; }

    [JsonPropertyName("printer_installed")]
    public bool PrinterInstalled { get; init; }

    [JsonPropertyName("webview2_version")]
    public string? WebView2Version { get; init; }

    [JsonPropertyName("last_desk_id")]
    public string? LastDeskId { get; init; }

    [JsonPropertyName("last_event_id")]
    public string? LastEventId { get; init; }

    [JsonPropertyName("last_registration_id")]
    public string? LastRegistrationId { get; init; }
}

public static class RelayDiagnosticsBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static RelayDiagnosticsSnapshot Build(
        RelaySessionSnapshot session,
        string appVersion,
        string? webView2Version = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(appVersion);

        return new RelayDiagnosticsSnapshot
        {
            AppVersion = appVersion,
            DeskName = session.DeskName,
            ApiHostname = session.ApiHostname,
            PrinterName = session.PrinterName,
            ConnectionState = session.ConnectionState.ToString(),
            LastSuccessfulPollUtc = session.LastSuccessfulPollUtc,
            LastPollLatencyMs = session.LastPollLatencyMs,
            LastPendingJobCount = session.LastPendingJobCount,
            JobsReceivedThisSession = session.JobsReceivedThisSession,
            JobsPrintedThisSession = session.JobsPrintedThisSession,
            JobsFailedThisSession = session.JobsFailedThisSession,
            LastJobId = session.LastJob?.JobId,
            LastJobOutcome = session.LastJob?.Outcome,
            LastErrorMessage = session.LastErrorMessage,
            PrinterInstalled = session.PrinterInstalled,
            WebView2Version = webView2Version,
            LastDeskId = session.LastJob?.DeskId,
            LastEventId = session.LastJob?.EventId,
            LastRegistrationId = session.LastJob?.RegistrationId,
        };
    }

    public static string ToJson(RelayDiagnosticsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);

        EnsureNoSecretLeak(json);

        return json;
    }

    public static RelayDiagnosticsSnapshot BuildFromSettings(
        RelaySettings settings,
        RelaySessionSnapshot session,
        string appVersion,
        string? webView2Version = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var snapshot = Build(session, appVersion, webView2Version);

        EnsureNoSecretLeak(snapshot.DeskName);
        EnsureNoSecretLeak(snapshot.ApiHostname);

        return snapshot;
    }

    internal static void EnsureNoSecretLeak(string value)
    {
        if (value.Contains("relay_", StringComparison.Ordinal)
            || value.Contains("DESK-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Diagnostics output must not contain relay secrets.");
        }
    }
}
