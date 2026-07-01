using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Polling;

namespace EventPlatform.PrintRelay.Core.Tests;

public sealed class PollBackoffTests
{
    [Fact]
    public void Reset_returns_to_poll_interval()
    {
        var backoff = new PollBackoff();

        Assert.Equal(RelayConstants.PollIntervalMs, backoff.CurrentDelayMs);

        Assert.Equal(2000, backoff.NextDelayMs());
        Assert.Equal(4000, backoff.NextDelayMs());

        backoff.Reset();

        Assert.Equal(RelayConstants.PollIntervalMs, backoff.CurrentDelayMs);
    }

    [Fact]
    public void NextDelay_caps_at_sixty_seconds()
    {
        var backoff = new PollBackoff();

        backoff.NextDelayMs();
        backoff.NextDelayMs();
        backoff.NextDelayMs();
        backoff.NextDelayMs();

        Assert.Equal(60_000, backoff.NextDelayMs());
        Assert.Equal(60_000, backoff.NextDelayMs());
    }
}

public sealed class PrintRelayApiClientTests
{
    [Fact]
    public void NormalizeApiUrl_strips_trailing_slash()
    {
        Assert.Equal(
            "https://app.example.com",
            PrintRelayApiClient.NormalizeApiUrl("https://app.example.com/"));
    }

    [Fact]
    public async Task GetPendingAsync_parses_jobs_array()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "jobs": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
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
            """);

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };

        var client = new PrintRelayApiClient(
            http,
            "https://app.example.com",
            "relay_test_secret");

        var pending = await client.GetPendingAsync();

        Assert.Single(pending.Jobs);
        Assert.Equal("queued", pending.Jobs[0].Status);
        Assert.Contains("<!DOCTYPE html>", pending.Jobs[0].BadgeHtml);
        Assert.Equal("Bearer relay_test_secret", handler.LastAuthorization);
    }

    [Fact]
    public async Task CompleteJobAsync_parses_response()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "status": "printed",
              "desk_id": "22222222-2222-2222-2222-222222222222",
              "event_id": "33333333-3333-3333-3333-333333333333",
              "registration_id": "44444444-4444-4444-4444-444444444444",
              "idempotency_key": "55555555-5555-5555-5555-555555555555",
              "is_reprint": false,
              "created_at": "2026-06-30T12:00:00.000Z",
              "printed_at": "2026-06-30T12:00:01.000Z",
              "failure_message": null
            }
            """);

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };

        var client = new PrintRelayApiClient(
            http,
            "https://app.example.com",
            "relay_test_secret");

        var result = await client.CompleteJobAsync(
            "11111111-1111-1111-1111-111111111111");

        Assert.Equal("printed", result.Status);
        Assert.Equal("2026-06-30T12:00:01.000Z", result.PrintedAt);
    }

    [Fact]
    public async Task FailJobAsync_truncates_message_at_500_chars()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "status": "failed",
              "desk_id": "22222222-2222-2222-2222-222222222222",
              "event_id": "33333333-3333-3333-3333-333333333333",
              "registration_id": "44444444-4444-4444-4444-444444444444",
              "idempotency_key": "55555555-5555-5555-5555-555555555555",
              "is_reprint": false,
              "created_at": "2026-06-30T12:00:00.000Z",
              "printed_at": null,
              "failure_message": "truncated"
            }
            """);

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };

        var client = new PrintRelayApiClient(
            http,
            "https://app.example.com",
            "relay_test_secret");

        var longMessage = new string('x', RelayConstants.MaxFailureMessageLength + 25);

        await client.FailJobAsync(
            "11111111-1111-1111-1111-111111111111",
            longMessage);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Equal(
            RelayConstants.MaxFailureMessageLength,
            System.Text.Json.JsonDocument.Parse(handler.LastRequestBody).RootElement
                .GetProperty("message")
                .GetString()!
                .Length);
    }

    [Fact]
    public async Task FailJobAsync_sends_message_body_only()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "status": "failed",
              "desk_id": "22222222-2222-2222-2222-222222222222",
              "event_id": "33333333-3333-3333-3333-333333333333",
              "registration_id": "44444444-4444-4444-4444-444444444444",
              "idempotency_key": "55555555-5555-5555-5555-555555555555",
              "is_reprint": false,
              "created_at": "2026-06-30T12:00:00.000Z",
              "printed_at": null,
              "failure_message": "Printer offline"
            }
            """);

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };

        var client = new PrintRelayApiClient(
            http,
            "https://app.example.com",
            "relay_test_secret");

        var result = await client.FailJobAsync(
            "11111111-1111-1111-1111-111111111111",
            "Printer offline");

        Assert.Equal("failed", result.Status);
        Assert.Contains("\"message\":\"Printer offline\"", handler.LastRequestBody);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public string? LastAuthorization { get; private set; }

        public string? LastRequestBody { get; private set; }

        public StubHttpMessageHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is not null)
            {
                LastAuthorization = request.Headers.Authorization.ToString();
            }

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
