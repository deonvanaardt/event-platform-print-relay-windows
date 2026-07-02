using System.Text.Json;
using System.Text.Json.Serialization;
using EventPlatform.PrintRelay.Core.Diagnostics;

namespace EventPlatform.PrintRelay.Core.Logging;

public sealed class RelayFileLogger : IRelayActivitySink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _logFilePath;
    private readonly long _maxLogBytes;
    private StreamWriter _writer;
    private readonly object _lock = new();

    public RelayFileLogger(string logFilePath, long maxLogBytes = RelayConstants.MaxRelayLogBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

        if (maxLogBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLogBytes), "Max log bytes must be positive.");
        }

        _logFilePath = logFilePath;
        _maxLogBytes = maxLogBytes;

        var directory = Path.GetDirectoryName(logFilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        RelayLogRetention.TruncateIfOversized(_logFilePath, _maxLogBytes);
        _writer = OpenWriter();
    }

    public static string GetDefaultLogPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "EventPlatform", "PrintRelay", "logs", "relay.log");
    }

    public void Record(RelayActivityEvent activityEvent)
    {
        ArgumentNullException.ThrowIfNull(activityEvent);

        var entry = new RelayLogEntry
        {
            Timestamp = activityEvent.TimestampUtc,
            Level = MapLevel(activityEvent.Kind),
            Message = activityEvent.Message,
            Kind = activityEvent.Kind.ToString(),
            JobId = activityEvent.JobId,
            DeskId = activityEvent.DeskId,
            EventId = activityEvent.EventId,
            RegistrationId = activityEvent.RegistrationId,
            PollLatencyMs = activityEvent.PollLatencyMs,
            PendingJobCount = activityEvent.PendingJobCount,
            FailureMessage = activityEvent.FailureMessage,
            BadgeHtmlPresent = activityEvent.BadgeHtmlPresent ? true : null,
        };

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        RelayDiagnosticsBuilder.EnsureNoSecretLeak(json);

        lock (_lock)
        {
            EnsureWithinSizeLimit();
            _writer.WriteLine(json);
        }
    }

    public void LogInfo(string message)
    {
        WriteDirect("info", message);
    }

    public void LogStopped()
    {
        WriteDirect("info", "Relay stopped.");
    }

    private void WriteDirect(string level, string message)
    {
        var entry = new RelayLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Message = message,
        };

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        lock (_lock)
        {
            EnsureWithinSizeLimit();
            _writer.WriteLine(json);
        }
    }

    private void EnsureWithinSizeLimit()
    {
        if (!File.Exists(_logFilePath))
        {
            return;
        }

        if (new FileInfo(_logFilePath).Length < _maxLogBytes)
        {
            return;
        }

        _writer.Dispose();
        RelayLogRetention.TruncateIfOversized(_logFilePath, _maxLogBytes);
        _writer = OpenWriter();

        var truncationNotice = JsonSerializer.Serialize(
            new RelayLogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = "info",
                Message = "Log truncated due to size limit.",
            },
            JsonOptions);

        _writer.WriteLine(truncationNotice);
    }

    private StreamWriter OpenWriter()
    {
        return new StreamWriter(
            new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true,
        };
    }

    private static string MapLevel(RelayActivityKind kind)
    {
        return kind switch
        {
            RelayActivityKind.PollFailed => "warn",
            RelayActivityKind.PrintFailed => "warn",
            RelayActivityKind.ConnectionStateChanged => "warn",
            RelayActivityKind.RelayStopped => "info",
            _ => "info",
        };
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}

internal sealed record RelayLogEntry
{
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("level")]
    public required string Level { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("job_id")]
    public string? JobId { get; init; }

    [JsonPropertyName("desk_id")]
    public string? DeskId { get; init; }

    [JsonPropertyName("event_id")]
    public string? EventId { get; init; }

    [JsonPropertyName("registration_id")]
    public string? RegistrationId { get; init; }

    [JsonPropertyName("poll_latency_ms")]
    public int? PollLatencyMs { get; init; }

    [JsonPropertyName("pending_job_count")]
    public int? PendingJobCount { get; init; }

    [JsonPropertyName("failure_message")]
    public string? FailureMessage { get; init; }

    [JsonPropertyName("badge_html_present")]
    public bool? BadgeHtmlPresent { get; init; }
}
