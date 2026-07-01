using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Polling;

namespace EventPlatform.PrintRelay.Core.Tests.Polling;

public sealed class PrintRelayPollLoopTests
{
  [Fact]
  public async Task Processes_jobs_in_created_at_order()
  {
    var handler = new SequenceHttpMessageHandler(
      """
      {
        "jobs": [
          {
            "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            "status": "queued",
            "desk_id": "22222222-2222-2222-2222-222222222222",
            "event_id": "33333333-3333-3333-3333-333333333333",
            "registration_id": "44444444-4444-4444-4444-444444444444",
            "idempotency_key": "55555555-5555-5555-5555-555555555555",
            "is_reprint": false,
            "created_at": "2026-06-30T13:00:00.000Z",
            "badge_document": {},
            "badge_html": "<!DOCTYPE html><html></html>"
          },
          {
            "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "status": "queued",
            "desk_id": "22222222-2222-2222-2222-222222222222",
            "event_id": "33333333-3333-3333-3333-333333333333",
            "registration_id": "44444444-4444-4444-4444-444444444444",
            "idempotency_key": "66666666-6666-6666-6666-666666666666",
            "is_reprint": false,
            "created_at": "2026-06-30T12:00:00.000Z",
            "badge_document": {},
            "badge_html": "<!DOCTYPE html><html></html>"
          }
        ]
      }
      """);

    var processor = new RecordingPrintJobProcessor(_ => PrintJobOutcome.Success());
    var delays = new RecordingDelay(stopAfterDelays: 1);
    using var harness = CreateHarness(handler, processor, delays);

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Equal(
      ["aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"],
      processor.ProcessedJobIds);
  }

  [Fact]
  public async Task Successful_job_calls_complete()
  {
    var handler = new SequenceHttpMessageHandler(PendingJobJson("job-1"));
    var processor = new RecordingPrintJobProcessor(_ => PrintJobOutcome.Success());
    var delays = new RecordingDelay(stopAfterDelays: 1);
    using var harness = CreateHarness(handler, processor, delays);

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Contains("/api/print-queue/job-1/complete", harness.Handler.PostedPaths);
    Assert.DoesNotContain(harness.Handler.PostedPaths, path => path.Contains("/failed"));
  }

  [Fact]
  public async Task Failed_job_calls_fail_with_message()
  {
    var handler = new SequenceHttpMessageHandler(PendingJobJson("job-1"));
    var processor = new RecordingPrintJobProcessor(
      _ => PrintJobOutcome.Failure("Printer offline"));
    var delays = new RecordingDelay(stopAfterDelays: 1);
    using var harness = CreateHarness(handler, processor, delays);

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Contains("/api/print-queue/job-1/failed", harness.Handler.PostedPaths);
    Assert.Contains(
      harness.Handler.FailBodies,
      body => body.Contains("\"message\":\"Printer offline\"", StringComparison.Ordinal));
  }

  [Fact]
  public async Task Processor_exception_does_not_stop_loop()
  {
    var handler = new SequenceHttpMessageHandler(
      PendingJobJson("job-1"),
      PendingJobJson("job-2"));
    var processor = new RecordingPrintJobProcessor(job =>
    {
      if (job.Id == "job-1")
      {
        throw new InvalidOperationException("Print driver crashed.");
      }

      return PrintJobOutcome.Success();
    });
    var delays = new RecordingDelay(stopAfterDelays: 2);
    using var harness = CreateHarness(handler, processor, delays);

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Contains("/api/print-queue/job-1/failed", harness.Handler.PostedPaths);
    Assert.Contains("/api/print-queue/job-2/complete", harness.Handler.PostedPaths);
    Assert.Equal(2, harness.Handler.PendingPollCount);
  }

  [Fact]
  public async Task Connectivity_failure_uses_backoff()
  {
    var handler = new SequenceHttpMessageHandler(
      new HttpRequestException("Network unreachable."),
      PendingJobJson());
    var processor = new RecordingPrintJobProcessor(_ => PrintJobOutcome.Success());
    var delays = new RecordingDelay(stopAfterDelays: 2);
    using var harness = CreateHarness(handler, processor, delays);

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Equal([2000, RelayConstants.PollIntervalMs], delays.RecordedDelaysMs);
    Assert.Equal(2, harness.Handler.PendingPollCount);
  }

  [Fact]
  public async Task Empty_queue_sleeps_poll_interval()
  {
    var handler = new SequenceHttpMessageHandler("""{ "jobs": [] }""");
    var processor = new RecordingPrintJobProcessor(_ => PrintJobOutcome.Success());
    var delays = new RecordingDelay(stopAfterDelays: 1);
    using var harness = CreateHarness(handler, processor, delays);

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Empty(processor.ProcessedJobIds);
    Assert.Equal([RelayConstants.PollIntervalMs], delays.RecordedDelaysMs);
  }

  [Fact]
  public async Task Auth_error_uses_poll_interval_without_backoff()
  {
    var handler = new SequenceHttpMessageHandler(
      new PrintRelayApiException("Unauthorized.", 401),
      PendingJobJson());
    var processor = new RecordingPrintJobProcessor(_ => PrintJobOutcome.Success());
    var delays = new RecordingDelay(stopAfterDelays: 2);
    var states = new List<PrintRelayPollConnectionState>();
    using var harness = CreateHarness(
      handler,
      processor,
      delays,
      state => states.Add(state));

    await harness.Loop.RunAsync(delays.CancellationToken);

    Assert.Equal(
      [RelayConstants.PollIntervalMs, RelayConstants.PollIntervalMs],
      delays.RecordedDelaysMs);
    Assert.Contains(PrintRelayPollConnectionState.AuthError, states);
    Assert.Contains(PrintRelayPollConnectionState.Connected, states);
  }

  private static PollLoopTestHarness CreateHarness(
    SequenceHttpMessageHandler handler,
    IPrintJobProcessor processor,
    RecordingDelay delays,
    Action<PrintRelayPollConnectionState>? onConnectionStateChanged = null)
  {
    return new PollLoopTestHarness(handler, processor, delays, onConnectionStateChanged);
  }

  private sealed class PollLoopTestHarness : IDisposable
  {
    private readonly HttpClient _http;

    public SequenceHttpMessageHandler Handler { get; }

    public PrintRelayPollLoop Loop { get; }

    public PollLoopTestHarness(
      SequenceHttpMessageHandler handler,
      IPrintJobProcessor processor,
      RecordingDelay delays,
      Action<PrintRelayPollConnectionState>? onConnectionStateChanged = null)
    {
      Handler = handler;
      _http = new HttpClient(handler)
      {
        BaseAddress = new Uri("https://app.example.com"),
      };

      var api = new PrintRelayApiClient(_http, "https://app.example.com", "relay_test_secret");

      Loop = new PrintRelayPollLoop(
        api,
        processor,
        new PollBackoff(),
        delays.DelayAsync,
        onConnectionStateChanged);
    }

    public void Dispose()
    {
      _http.Dispose();
    }
  }

  private static string PendingJobJson(string jobId = "11111111-1111-1111-1111-111111111111")
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

  private sealed class RecordingPrintJobProcessor : IPrintJobProcessor
  {
    private readonly Func<PrintQueuePendingJob, PrintJobOutcome> _handler;

    public List<string> ProcessedJobIds { get; } = [];

    public RecordingPrintJobProcessor(Func<PrintQueuePendingJob, PrintJobOutcome> handler)
    {
      _handler = handler;
    }

    public Task<PrintJobOutcome> ProcessAsync(
      PrintQueuePendingJob job,
      CancellationToken cancellationToken = default)
    {
      ProcessedJobIds.Add(job.Id);
      return Task.FromResult(_handler(job));
    }
  }

  private sealed class RecordingDelay
  {
    private readonly int _stopAfterDelays;
    private int _delayCount;
    private readonly CancellationTokenSource _cts = new();

    public RecordingDelay(int stopAfterDelays)
    {
      _stopAfterDelays = stopAfterDelays;
    }

    public List<int> RecordedDelaysMs { get; } = [];

    public CancellationToken CancellationToken => _cts.Token;

    public Task DelayAsync(int delayMs, CancellationToken cancellationToken)
    {
      RecordedDelaysMs.Add(delayMs);
      _delayCount++;

      if (_delayCount >= _stopAfterDelays)
      {
        _cts.Cancel();
      }

      return Task.CompletedTask;
    }
  }

  private sealed class SequenceHttpMessageHandler : HttpMessageHandler
  {
    private readonly Queue<object> _steps = new();
    private int _pendingPollCount;

    public int PendingPollCount => _pendingPollCount;

    public List<string> PostedPaths { get; } = [];

    public List<string> FailBodies { get; } = [];

    public SequenceHttpMessageHandler(params object[] steps)
    {
      foreach (var step in steps)
      {
        _steps.Enqueue(step);
      }
    }

    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      if (request.Method == HttpMethod.Get
          && request.RequestUri?.AbsolutePath == "/api/print-queue/pending")
      {
        _pendingPollCount++;

        if (_steps.Count == 0)
        {
          return Task.FromResult(CreateJsonResponse("""{ "jobs": [] }"""));
        }

        var step = _steps.Dequeue();

        if (step is Exception exception)
        {
          return Task.FromException<HttpResponseMessage>(exception);
        }

        return Task.FromResult(CreateJsonResponse((string)step));
      }

      if (request.Method == HttpMethod.Post)
      {
        PostedPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);

        if (request.RequestUri?.AbsolutePath.EndsWith("/failed", StringComparison.Ordinal) == true
            && request.Content is not null)
        {
          return ReadFailBodyAsync(request, cancellationToken);
        }

        return Task.FromResult(CreateJsonResponse(TerminalJobJson("printed")));
      }

      return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private async Task<HttpResponseMessage> ReadFailBodyAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      var body = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      FailBodies.Add(body);
      return CreateJsonResponse(TerminalJobJson("failed"));
    }

    private static HttpResponseMessage CreateJsonResponse(string body)
    {
      return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
      {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
      };
    }

    private static string TerminalJobJson(string status)
    {
      return $$"""
        {
          "id": "11111111-1111-1111-1111-111111111111",
          "status": "{{status}}",
          "desk_id": "22222222-2222-2222-2222-222222222222",
          "event_id": "33333333-3333-3333-3333-333333333333",
          "registration_id": "44444444-4444-4444-4444-444444444444",
          "idempotency_key": "55555555-5555-5555-5555-555555555555",
          "is_reprint": false,
          "created_at": "2026-06-30T12:00:00.000Z",
          "printed_at": null,
          "failure_message": null
        }
        """;
    }
  }
}
