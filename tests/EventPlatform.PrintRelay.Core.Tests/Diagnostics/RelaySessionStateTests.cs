using EventPlatform.PrintRelay.Core.Diagnostics;
using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.Core.Tests.Diagnostics;

public sealed class RelaySessionStateTests
{
  private static RelaySettings TestSettings() =>
      new()
      {
        Secret = "relay_test_secret_value",
        ApiUrl = "https://staging.example.com",
        DeskName = "Main entrance",
        PrinterName = "Test Printer",
      };

  [Fact]
  public void Records_poll_success_and_updates_counters()
  {
    var state = new RelaySessionState();
    var timestamp = DateTimeOffset.UtcNow;

    state.Record(new RelayActivityEvent
    {
      TimestampUtc = timestamp,
      Kind = RelayActivityKind.PollSucceeded,
      Message = "Polling… 0 jobs pending (120 ms)",
      PollLatencyMs = 120,
      PendingJobCount = 0,
    });

    var snapshot = state.GetSnapshot(TestSettings());

    Assert.Equal(PrintRelayPollConnectionState.Connected, snapshot.ConnectionState);
    Assert.Equal(timestamp, snapshot.LastSuccessfulPollUtc);
    Assert.Equal(120, snapshot.LastPollLatencyMs);
    Assert.Equal(0, snapshot.LastPendingJobCount);
  }

  [Fact]
  public void Ring_buffer_trims_activity_events()
  {
    var state = new RelaySessionState();

    for (var index = 0; index < RelaySessionState.MaxActivityEvents + 5; index++)
    {
      state.Record(new RelayActivityEvent
      {
        TimestampUtc = DateTimeOffset.UtcNow,
        Kind = RelayActivityKind.PollSucceeded,
        Message = $"Poll {index}",
        PollLatencyMs = 1,
        PendingJobCount = 0,
      });
    }

    var snapshot = state.GetSnapshot(TestSettings());

    Assert.Equal(RelaySessionState.MaxActivityEvents, snapshot.ActivityEvents.Count);
    Assert.Equal("Poll 5", snapshot.ActivityEvents[0].Message);
  }

  [Fact]
  public void Job_lifecycle_updates_recent_jobs_and_counters()
  {
    var state = new RelaySessionState();
    var jobId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    state.Record(new RelayActivityEvent
    {
      TimestampUtc = DateTimeOffset.UtcNow,
      Kind = RelayActivityKind.JobReceived,
      Message = "Received job",
      JobId = jobId,
      DeskId = "22222222-2222-2222-2222-222222222222",
      EventId = "33333333-3333-3333-3333-333333333333",
      RegistrationId = "44444444-4444-4444-4444-444444444444",
      BadgeHtmlPresent = true,
    });

    state.Record(new RelayActivityEvent
    {
      TimestampUtc = DateTimeOffset.UtcNow,
      Kind = RelayActivityKind.PrintCompleted,
      Message = "Printed job",
      JobId = jobId,
    });

    var snapshot = state.GetSnapshot(TestSettings());

    Assert.Equal(1, snapshot.JobsReceivedThisSession);
    Assert.Equal(1, snapshot.JobsPrintedThisSession);
    Assert.Equal("printed", snapshot.LastJob?.Outcome);
    Assert.Single(snapshot.RecentJobs);
  }

  [Fact]
  public void Changed_event_fires_on_record()
  {
    var state = new RelaySessionState();
    var changes = 0;
    state.Changed += () => changes++;

    state.Record(new RelayActivityEvent
    {
      TimestampUtc = DateTimeOffset.UtcNow,
      Kind = RelayActivityKind.PollSucceeded,
      Message = "Poll",
      PollLatencyMs = 1,
      PendingJobCount = 0,
    });

    Assert.Equal(1, changes);
  }
}
