using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Pairing;

namespace EventPlatform.PrintRelay.Core.Tests.Pairing;

public sealed class PairingExchangeClientTests
{
    private const string SuccessBody =
        """
        {
          "secret": "relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5",
          "api_url": "https://app.kiosa.io",
          "desk_name": "Main entrance",
          "desk_id": "44444444-4444-4444-8444-444444444444"
        }
        """;

    [Fact]
    public async Task ExchangeAsync_returns_parsed_result_on_200()
    {
        var handler = new StatusHttpMessageHandler(SuccessBody, System.Net.HttpStatusCode.OK);
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.kiosa.io"),
        };

        var client = new PairingExchangeClient(http, "https://app.kiosa.io");
        var result = await client.ExchangeAsync("K7MNP2QR");

        Assert.Equal("relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5", result.Secret);
        Assert.Equal("https://app.kiosa.io", result.ApiUrl);
        Assert.Equal("Main entrance", result.DeskName);
        Assert.Equal("44444444-4444-4444-8444-444444444444", result.DeskId);
        Assert.Equal("/api/v1/print-desks/pair", handler.LastRequestPath);
        Assert.Contains("\"code\":\"K7MNP2QR\"", handler.LastRequestBody);
        Assert.Null(handler.LastAuthorization);
    }

    [Fact]
    public async Task ExchangeAsync_throws_on_400()
    {
        var handler = new StatusHttpMessageHandler(
            """{"error":"INVALID_INPUT","message":"Invalid code"}""",
            System.Net.HttpStatusCode.BadRequest);
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.kiosa.io"),
        };

        var client = new PairingExchangeClient(http, "https://app.kiosa.io");

        var ex = await Assert.ThrowsAsync<PrintRelayApiException>(
            () => client.ExchangeAsync("K7MNP2QR"));

        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task ExchangeAsync_throws_on_429()
    {
        var handler = new StatusHttpMessageHandler(
            """{"error":"RATE_LIMIT_EXCEEDED","message":"Too many attempts"}""",
            System.Net.HttpStatusCode.TooManyRequests);
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.kiosa.io"),
        };

        var client = new PairingExchangeClient(http, "https://app.kiosa.io");

        var ex = await Assert.ThrowsAsync<PrintRelayApiException>(
            () => client.ExchangeAsync("K7MNP2QR"));

        Assert.Equal(429, ex.Status);
    }

    [Fact]
    public async Task ExchangeAsync_throws_on_invalid_success_body()
    {
        var handler = new StatusHttpMessageHandler(
            """{"secret":"not_relay","api_url":"https://app.kiosa.io","desk_name":"X","desk_id":"44444444-4444-4444-8444-444444444444"}""",
            System.Net.HttpStatusCode.OK);
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.kiosa.io"),
        };

        var client = new PairingExchangeClient(http, "https://app.kiosa.io");

        await Assert.ThrowsAsync<PrintRelayApiException>(
            () => client.ExchangeAsync("K7MNP2QR"));
    }

    private sealed class StatusHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly System.Net.HttpStatusCode _statusCode;

        public string? LastRequestPath { get; private set; }

        public string? LastRequestBody { get; private set; }

        public string? LastAuthorization { get; private set; }

        public StatusHttpMessageHandler(string responseBody, System.Net.HttpStatusCode statusCode)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestPath = request.RequestUri?.AbsolutePath;
            LastAuthorization = request.Headers.Authorization?.ToString();

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
