using EventPlatform.PrintRelay.Core.Diagnostics;
using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.Core.Tests.Diagnostics;

public sealed class RelayDiagnosticsBuilderTests
{
  private static RelaySessionSnapshot SampleSession() =>
      new()
      {
        DeskName = "Main entrance",
        ApiHostname = "staging.example.com",
        PrinterName = "Test Printer",
        ConnectionState = PrintRelayPollConnectionState.Connected,
        LastSuccessfulPollUtc = DateTimeOffset.Parse("2026-07-01T12:00:00Z"),
        LastPollLatencyMs = 95,
        LastPendingJobCount = 0,
        JobsReceivedThisSession = 1,
        JobsPrintedThisSession = 1,
        JobsFailedThisSession = 0,
        LastJob = new RelayJobSummary
        {
          JobId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          DeskId = "22222222-2222-2222-2222-222222222222",
          EventId = "33333333-3333-3333-3333-333333333333",
          RegistrationId = "44444444-4444-4444-4444-444444444444",
          ReceivedAtUtc = DateTimeOffset.Parse("2026-07-01T12:01:00Z"),
          Outcome = "printed",
        },
        LastErrorMessage = null,
        PrinterInstalled = true,
        ActivityEvents = [],
        RecentJobs = [],
      };

  [Fact]
  public void Build_includes_technical_ids_for_support()
  {
    var diagnostics = RelayDiagnosticsBuilder.Build(
      SampleSession(),
      appVersion: "0.2.0",
      webView2Version: "131.0.2903.40");

    Assert.Equal("0.2.0", diagnostics.AppVersion);
    Assert.Equal("staging.example.com", diagnostics.ApiHostname);
    Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", diagnostics.LastJobId);
    Assert.Equal("22222222-2222-2222-2222-222222222222", diagnostics.LastDeskId);
    Assert.Equal("printed", diagnostics.LastJobOutcome);
  }

  [Fact]
  public void ToJson_never_contains_secret_markers()
  {
    var json = RelayDiagnosticsBuilder.ToJson(
      RelayDiagnosticsBuilder.Build(SampleSession(), "0.2.0"));

    Assert.DoesNotContain("relay_", json, StringComparison.Ordinal);
    Assert.DoesNotContain("DESK-", json, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void EnsureNoSecretLeak_throws_when_secret_present()
  {
    Assert.Throws<InvalidOperationException>(
      () => RelayDiagnosticsBuilder.EnsureNoSecretLeak("Bearer relay_abc123"));
  }

  [Fact]
  public void ApiHostname_extracts_host_only()
  {
    Assert.Equal(
      "staging.example.com",
      RelayApiHostname.FromApiUrl("https://staging.example.com/"));
  }
}
