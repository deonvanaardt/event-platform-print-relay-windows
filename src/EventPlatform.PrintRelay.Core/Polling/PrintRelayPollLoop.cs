using EventPlatform.PrintRelay.Core.Api;

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

    public PrintRelayPollLoop(
        PrintRelayApiClient api,
        IPrintJobProcessor processor,
        PollBackoff backoff,
        Func<int, CancellationToken, Task>? delayAsync = null,
        Action<PrintRelayPollConnectionState>? onConnectionStateChanged = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _backoff = backoff ?? throw new ArgumentNullException(nameof(backoff));
        _delayAsync = delayAsync ?? DefaultDelayAsync;
        _onConnectionStateChanged = onConnectionStateChanged;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pending = await _api.GetPendingAsync(cancellationToken).ConfigureAwait(false);
                _backoff.Reset();
                SetConnectionState(PrintRelayPollConnectionState.Connected);

                var jobs = pending.Jobs
                    .OrderBy(job => job.CreatedAt, StringComparer.Ordinal)
                    .ToList();

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
                await _delayAsync(RelayConstants.PollIntervalMs, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (PrintRelayApiException)
            {
                await _delayAsync(RelayConstants.PollIntervalMs, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex, cancellationToken))
            {
                SetConnectionState(PrintRelayPollConnectionState.BackingOff);
                var delayMs = _backoff.NextDelayMs();
                await _delayAsync(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessJobSafelyAsync(
        PrintQueuePendingJob job,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await _processor
                .ProcessAsync(job, cancellationToken)
                .ConfigureAwait(false);

            if (outcome.Succeeded)
            {
                await TryCompleteJobAsync(job.Id, cancellationToken).ConfigureAwait(false);
                return;
            }

            var message = string.IsNullOrWhiteSpace(outcome.FailureMessage)
                ? UnexpectedJobFailureMessage
                : outcome.FailureMessage;

            await TryFailJobAsync(job.Id, message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
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
        }
        catch (Exception)
        {
            // Never crash the loop when fail acknowledgement fails.
        }
    }

    private void SetConnectionState(PrintRelayPollConnectionState state)
    {
        _onConnectionStateChanged?.Invoke(state);
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
