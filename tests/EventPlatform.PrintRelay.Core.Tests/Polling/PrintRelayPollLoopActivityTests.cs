using EventPlatform.PrintRelay.Core.Diagnostics;
using EventPlatform.PrintRelay.Core.Polling;

namespace EventPlatform.PrintRelay.Core.Tests.Polling;

public sealed class PrintRelayPollLoopActivityTests
{
  [Fact]
  public async Task Emits_poll_and_job_activity_events()
  {
    var handler = new ActivityTestHttpHandler(PendingJobJson("job-activity-1"));
    var processor = new SuccessProcessor();
    var delays = new ActivityRecordingDelay(stopAfterDelays: 1);
    var sink = new RecordingActivitySink();

    using var http = new HttpClient(handler) { BaseAddress = new Uri("https://app.example.com") };
    var api = new Core.Api.PrintRelayApiClient(http, "https://app.example.com", "relay_test_secret");
    var loop = new PrintRelayPollLoop(
      api,
      processor,
      new PollBackoff(),
      delays.DelayAsync,
      activitySink: sink);

    await loop.RunAsync(delays.CancellationToken);

    Assert.Contains(
      sink.Events,
      activity => activity.Kind == RelayActivityKind.PollSucceeded
        && activity.PendingJobCount == 1);

    Assert.Contains(
      sink.Events,
      activity => activity.Kind == RelayActivityKind.JobReceived
        && activity.JobId == "job-activity-1");

    Assert.Contains(
      sink.Events,
      activity => activity.Kind == RelayActivityKind.PrintCompleted
        && activity.JobId == "job-activity-1");
  }

  private static string PendingJobJson(string jobId)
  {
    return $$"""
      {
        "jobs": [
          {
            "id": "{{jobId}}",
            "status": "queued",
            "desk_id": "22222222-2222-2222-2222-222222222222",
            "event_id": "33333333-3333-3333-3333-333333333333",
            "registration_id": "44444444-4444-4444-4444-444444444444",
            "idempotency_key": "55555555-5555-5555-5555-555555555555",
            "is_reprint": false,
            "created_at": "2026-06-30T12:00:00.000Z",
            "badge_document": {},
            "badge_html": "<!DOCTYPE html><html></html>"
          }
        ]
      }
      """;
  }

  private sealed class RecordingActivitySink : IRelayActivitySink
  {
    public List<RelayActivityEvent> Events { get; } = [];

    public void Record(RelayActivityEvent activityEvent) => Events.Add(activityEvent);
  }

  private sealed class SuccessProcessor : IPrintJobProcessor
  {
    public Task<PrintJobOutcome> ProcessAsync(
      Core.Api.PrintQueuePendingJob job,
      CancellationToken cancellationToken = default) =>
        Task.FromResult(PrintJobOutcome.Success());
  }

  private sealed class ActivityRecordingDelay
  {
    private readonly int _stopAfterDelays;
    private int _delayCount;
    private readonly CancellationTokenSource _cts = new();

    public ActivityRecordingDelay(int stopAfterDelays) => _stopAfterDelays = stopAfterDelays;

    public CancellationToken CancellationToken => _cts.Token;

    public Task DelayAsync(int delayMs, CancellationToken cancellationToken)
    {
      _delayCount++;

      if (_delayCount >= _stopAfterDelays)
      {
        _cts.Cancel();
      }

      return Task.CompletedTask;
    }
  }

  private sealed class ActivityTestHttpHandler : HttpMessageHandler
  {
    private readonly string _pendingJson;
    private bool _served;

    public ActivityTestHttpHandler(string pendingJson) => _pendingJson = pendingJson;

    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      if (request.Method == HttpMethod.Get
          && request.RequestUri?.AbsolutePath == "/api/print-queue/pending")
      {
        _served = true;
        return Task.FromResult(JsonResponse(_served ? _pendingJson : """{ "jobs": [] }"""));
      }

      if (request.Method == HttpMethod.Post)
      {
        return Task.FromResult(JsonResponse("""{"id":"job-activity-1","status":"printed"}"""));
      }

      return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage JsonResponse(string body) =>
        new(System.Net.HttpStatusCode.OK)
        {
          Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
  }
}
