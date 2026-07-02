using System.Diagnostics;
using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Diagnostics;

namespace EventPlatform.PrintRelay.Core.Polling;

public sealed class PrintRelayPollLoop
{
    private const string UnexpectedJobFailureMessage =
        "The badge could not be printed. Try again or contact support.";

    private readonly PrintRelayApiClient _api;
    private readonly IPrintJobProcessor _processor;
    private readonly PollBackoff _backoff;
    private readonly Func<int, CancellationToken, Task> _delayAsync;
    private readonly Action<PrintRelayPollConnectionState>? _onConnectionStateChanged;
    private readonly IRelayActivitySink? _activitySink;
    private PrintRelayPollConnectionState _connectionState = PrintRelayPollConnectionState.Connected;

    public PrintRelayPollLoop(
        PrintRelayApiClient api,
        IPrintJobProcessor processor,
        PollBackoff backoff,
        Func<int, CancellationToken, Task>? delayAsync = null,
        Action<PrintRelayPollConnectionState>? onConnectionStateChanged = null,
        IRelayActivitySink? activitySink = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _backoff = backoff ?? throw new ArgumentNullException(nameof(backoff));
        _delayAsync = delayAsync ?? DefaultDelayAsync;
        _onConnectionStateChanged = onConnectionStateChanged;
        _activitySink = activitySink;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var pending = await _api.GetPendingAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                _backoff.Reset();
                SetConnectionState(PrintRelayPollConnectionState.Connected);

                var jobs = pending.Jobs
                    .OrderBy(job => job.CreatedAt, StringComparer.Ordinal)
                    .ToList();

                RecordActivity(new RelayActivityEvent
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Kind = RelayActivityKind.PollSucceeded,
                    Message = RelayActivityMessages.PollSucceeded(jobs.Count, (int)stopwatch.ElapsedMilliseconds),
                    PollLatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    PendingJobCount = jobs.Count,
                });

                foreach (var job in jobs)
                {
                    await ProcessJobSafelyAsync(job, cancellationToken).ConfigureAwait(false);
                }

                await _delayAsync(RelayConstants.PollIntervalMs, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (PrintRelayApiException ex) when (IsAuthError(ex))
            {
                SetConnectionState(PrintRelayPollConnectionState.AuthError);
                RecordActivity(new RelayActivityEvent
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Kind = RelayActivityKind.PollFailed,
                    Message = RelayActivityMessages.ConnectionState(PrintRelayPollConnectionState.AuthError),
                });

                await _delayAsync(RelayConstants.PollIntervalMs, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (PrintRelayApiException)
            {
                RecordActivity(new RelayActivityEvent
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Kind = RelayActivityKind.PollFailed,
                    Message = RelayActivityMessages.PollFailed("platform returned an error"),
                });

                await _delayAsync(RelayConstants.PollIntervalMs, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex, cancellationToken))
            {
                SetConnectionState(PrintRelayPollConnectionState.BackingOff);
                RecordActivity(new RelayActivityEvent
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Kind = RelayActivityKind.PollFailed,
                    Message = RelayActivityMessages.PollFailed("could not reach the platform"),
                });

                var delayMs = _backoff.NextDelayMs();
                await _delayAsync(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessJobSafelyAsync(
        PrintQueuePendingJob job,
        CancellationToken cancellationToken)
    {
        RecordActivity(new RelayActivityEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = RelayActivityKind.JobReceived,
            Message = RelayActivityMessages.JobReceived(
                RelayActivityMessages.IdSuffix(job.RegistrationId)),
            JobId = job.Id,
            DeskId = job.DeskId,
            EventId = job.EventId,
            RegistrationId = job.RegistrationId,
            BadgeHtmlPresent = !string.IsNullOrWhiteSpace(job.BadgeHtml),
        });

        RecordActivity(new RelayActivityEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = RelayActivityKind.PrintStarted,
            Message = RelayActivityMessages.PrintStarted(RelayActivityMessages.IdSuffix(job.Id)),
            JobId = job.Id,
            DeskId = job.DeskId,
            EventId = job.EventId,
            RegistrationId = job.RegistrationId,
        });

        try
        {
            var outcome = await _processor
                .ProcessAsync(job, cancellationToken)
                .ConfigureAwait(false);

            if (outcome.Succeeded)
            {
                RecordActivity(new RelayActivityEvent
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Kind = RelayActivityKind.PrintCompleted,
                    Message = RelayActivityMessages.PrintCompleted(
                        RelayActivityMessages.IdSuffix(job.Id)),
                    JobId = job.Id,
                    DeskId = job.DeskId,
                    EventId = job.EventId,
                    RegistrationId = job.RegistrationId,
                });

                await TryCompleteJobAsync(job.Id, cancellationToken).ConfigureAwait(false);
                return;
            }

            var message = string.IsNullOrWhiteSpace(outcome.FailureMessage)
                ? UnexpectedJobFailureMessage
                : outcome.FailureMessage;

            RecordActivity(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.PrintFailed,
                Message = RelayActivityMessages.PrintFailed(
                    RelayActivityMessages.IdSuffix(job.Id),
                    message),
                JobId = job.Id,
                DeskId = job.DeskId,
                EventId = job.EventId,
                RegistrationId = job.RegistrationId,
                FailureMessage = message,
            });

            await TryFailJobAsync(job.Id, message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            RecordActivity(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.PrintFailed,
                Message = RelayActivityMessages.PrintFailed(
                    RelayActivityMessages.IdSuffix(job.Id),
                    UnexpectedJobFailureMessage),
                JobId = job.Id,
                DeskId = job.DeskId,
                EventId = job.EventId,
                RegistrationId = job.RegistrationId,
                FailureMessage = UnexpectedJobFailureMessage,
            });

            await TryFailJobAsync(
                    job.Id,
                    UnexpectedJobFailureMessage,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task TryCompleteJobAsync(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            await _api.CompleteJobAsync(jobId, cancellationToken).ConfigureAwait(false);

            RecordActivity(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.JobAcknowledged,
                Message = RelayActivityMessages.JobAcknowledged(
                    RelayActivityMessages.IdSuffix(jobId),
                    "printed"),
                JobId = jobId,
            });
        }
        catch (Exception)
        {
            // Never crash the loop when complete acknowledgement fails.
        }
    }

    private async Task TryFailJobAsync(
        string jobId,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _api.FailJobAsync(jobId, message, cancellationToken).ConfigureAwait(false);

            RecordActivity(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.JobAcknowledged,
                Message = RelayActivityMessages.JobAcknowledged(
                    RelayActivityMessages.IdSuffix(jobId),
                    "failed"),
                JobId = jobId,
                FailureMessage = message,
            });
        }
        catch (Exception)
        {
            // Never crash the loop when fail acknowledgement fails.
        }
    }

    private void SetConnectionState(PrintRelayPollConnectionState state)
    {
        if (_connectionState == state)
        {
            _onConnectionStateChanged?.Invoke(state);
            return;
        }

        _connectionState = state;
        _onConnectionStateChanged?.Invoke(state);

        RecordActivity(new RelayActivityEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = RelayActivityKind.ConnectionStateChanged,
            Message = RelayActivityMessages.ConnectionState(state),
            ConnectionState = state,
        });
    }

    private void RecordActivity(RelayActivityEvent activityEvent)
    {
        _activitySink?.Record(activityEvent);
    }

    private static bool IsAuthError(PrintRelayApiException ex)
    {
        return ex.Status is 401 or 403;
    }

    private static bool IsConnectivityFailure(Exception ex, CancellationToken cancellationToken)
    {
        if (ex is HttpRequestException)
        {
            return true;
        }

        if (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return true;
        }

        if (ex is PrintRelayApiException apiException && apiException.Status >= 500)
        {
            return true;
        }

        return false;
    }

    private static Task DefaultDelayAsync(int delayMs, CancellationToken cancellationToken)
    {
        return Task.Delay(delayMs, cancellationToken);
    }
}
