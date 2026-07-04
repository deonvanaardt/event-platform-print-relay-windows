using System.Diagnostics;
using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Diagnostics;

namespace EventPlatform.PrintRelay.Core.Polling;

public static class RelayConnectionTester
{
    public static async Task TestConnectionAsync(
        PrintRelayApiClient api,
        IRelayActivitySink activitySink,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(activitySink);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var pending = await api.GetPendingAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            activitySink.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.ManualConnectionTest,
                Message = RelayActivityMessages.ManualConnectionTest(
                    pending.Jobs.Count,
                    (int)stopwatch.ElapsedMilliseconds),
                PollLatencyMs = (int)stopwatch.ElapsedMilliseconds,
                PendingJobCount = pending.Jobs.Count,
            });
        }
        catch (PrintRelayApiException ex) when (ex.Status is 401 or 403)
        {
            activitySink.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.PollFailed,
                Message = RelayActivityMessages.ConnectionState(PrintRelayPollConnectionState.AuthError),
                ConnectionState = PrintRelayPollConnectionState.AuthError,
            });

            activitySink.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.ConnectionStateChanged,
                Message = RelayActivityMessages.ConnectionState(PrintRelayPollConnectionState.AuthError),
                ConnectionState = PrintRelayPollConnectionState.AuthError,
            });
        }
        catch (Exception)
        {
            activitySink.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.PollFailed,
                Message = RelayActivityMessages.PollFailed("could not reach the platform"),
            });
        }
    }
}
