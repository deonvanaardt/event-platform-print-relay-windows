using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.Core.Diagnostics;

public sealed class RelaySessionState : IRelayActivitySink
{
    public const int MaxActivityEvents = 50;

    public const int MaxRecentJobs = 10;

    private readonly object _lock = new();

    private readonly List<RelayActivityEvent> _activityEvents = [];

    private readonly List<RelayJobSummary> _recentJobs = [];

    private PrintRelayPollConnectionState _connectionState = PrintRelayPollConnectionState.Connected;

    private DateTimeOffset? _lastSuccessfulPollUtc;

    private int? _lastPollLatencyMs;

    private int _lastPendingJobCount;

    private RelayJobSummary? _lastJob;

    private string? _lastErrorMessage;

    private int _jobsReceivedThisSession;

    private int _jobsPrintedThisSession;

    private int _jobsFailedThisSession;

    private bool _printerInstalled = true;

    public event Action? Changed;

    public RelaySessionSnapshot GetSnapshot(RelaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_lock)
        {
            return new RelaySessionSnapshot
            {
                DeskName = settings.DeskName,
                ApiHostname = RelayApiHostname.FromApiUrl(settings.ApiUrl),
                PrinterName = settings.PrinterName,
                ConnectionState = _connectionState,
                LastSuccessfulPollUtc = _lastSuccessfulPollUtc,
                LastPollLatencyMs = _lastPollLatencyMs,
                LastPendingJobCount = _lastPendingJobCount,
                JobsReceivedThisSession = _jobsReceivedThisSession,
                JobsPrintedThisSession = _jobsPrintedThisSession,
                JobsFailedThisSession = _jobsFailedThisSession,
                LastJob = _lastJob,
                LastErrorMessage = _lastErrorMessage,
                PrinterInstalled = _printerInstalled,
                ActivityEvents = _activityEvents.ToList(),
                RecentJobs = _recentJobs.ToList(),
            };
        }
    }

    public void SetPrinterInstalled(bool installed)
    {
        lock (_lock)
        {
            _printerInstalled = installed;
        }

        RaiseChanged();
    }

    public void Record(RelayActivityEvent activityEvent)
    {
        ArgumentNullException.ThrowIfNull(activityEvent);

        lock (_lock)
        {
            _activityEvents.Add(activityEvent);

            if (_activityEvents.Count > MaxActivityEvents)
            {
                _activityEvents.RemoveRange(0, _activityEvents.Count - MaxActivityEvents);
            }

            switch (activityEvent.Kind)
            {
                case RelayActivityKind.PollSucceeded:
                case RelayActivityKind.ManualConnectionTest:
                    _lastSuccessfulPollUtc = activityEvent.TimestampUtc;
                    _lastPollLatencyMs = activityEvent.PollLatencyMs;
                    _lastPendingJobCount = activityEvent.PendingJobCount ?? 0;
                    _connectionState = PrintRelayPollConnectionState.Connected;
                    break;

                case RelayActivityKind.PollFailed:
                    _lastErrorMessage = activityEvent.Message;
                    break;

                case RelayActivityKind.ConnectionStateChanged
                    when activityEvent.ConnectionState.HasValue:
                    _connectionState = activityEvent.ConnectionState.Value;

                    if (_connectionState == PrintRelayPollConnectionState.AuthError)
                    {
                        _lastErrorMessage = activityEvent.Message;
                    }

                    break;

                case RelayActivityKind.JobReceived:
                    _jobsReceivedThisSession++;

                    if (activityEvent.JobId is not null
                        && activityEvent.DeskId is not null
                        && activityEvent.EventId is not null
                        && activityEvent.RegistrationId is not null)
                    {
                        var summary = new RelayJobSummary
                        {
                            JobId = activityEvent.JobId,
                            DeskId = activityEvent.DeskId,
                            EventId = activityEvent.EventId,
                            RegistrationId = activityEvent.RegistrationId,
                            ReceivedAtUtc = activityEvent.TimestampUtc,
                        };

                        _lastJob = summary;
                        InsertRecentJob(summary);
                    }

                    break;

                case RelayActivityKind.PrintCompleted:
                    _jobsPrintedThisSession++;
                    UpdateLastJobOutcome(activityEvent, "printed");
                    break;

                case RelayActivityKind.PrintFailed:
                    _jobsFailedThisSession++;
                    _lastErrorMessage = activityEvent.FailureMessage ?? activityEvent.Message;
                    UpdateLastJobOutcome(activityEvent, "failed", activityEvent.FailureMessage);
                    break;

                case RelayActivityKind.JobAcknowledged:
                    break;
            }
        }

        RaiseChanged();
    }

    private void UpdateLastJobOutcome(
        RelayActivityEvent activityEvent,
        string outcome,
        string? failureMessage = null)
    {
        if (activityEvent.JobId is null)
        {
            return;
        }

        if (_lastJob?.JobId == activityEvent.JobId)
        {
            _lastJob = _lastJob with
            {
                Outcome = outcome,
                FailureMessage = failureMessage,
            };
        }

        for (var index = 0; index < _recentJobs.Count; index++)
        {
            if (_recentJobs[index].JobId != activityEvent.JobId)
            {
                continue;
            }

            _recentJobs[index] = _recentJobs[index] with
            {
                Outcome = outcome,
                FailureMessage = failureMessage,
            };

            break;
        }
    }

    private void InsertRecentJob(RelayJobSummary summary)
    {
        _recentJobs.Insert(0, summary);

        if (_recentJobs.Count > MaxRecentJobs)
        {
            _recentJobs.RemoveRange(MaxRecentJobs, _recentJobs.Count - MaxRecentJobs);
        }
    }

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }
}

public sealed record RelaySessionSnapshot
{
    public required string DeskName { get; init; }

    public required string ApiHostname { get; init; }

    public required string PrinterName { get; init; }

    public required PrintRelayPollConnectionState ConnectionState { get; init; }

    public DateTimeOffset? LastSuccessfulPollUtc { get; init; }

    public int? LastPollLatencyMs { get; init; }

    public int LastPendingJobCount { get; init; }

    public int JobsReceivedThisSession { get; init; }

    public int JobsPrintedThisSession { get; init; }

    public int JobsFailedThisSession { get; init; }

    public RelayJobSummary? LastJob { get; init; }

    public string? LastErrorMessage { get; init; }

    public bool PrinterInstalled { get; init; }

    public required IReadOnlyList<RelayActivityEvent> ActivityEvents { get; init; }

    public required IReadOnlyList<RelayJobSummary> RecentJobs { get; init; }
}
