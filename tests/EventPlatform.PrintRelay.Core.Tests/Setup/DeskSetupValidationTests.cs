using EventPlatform.PrintRelay.Core.Setup;
using EventPlatform.PrintRelay.Core.SetupCode;

namespace EventPlatform.PrintRelay.Core.Tests.Setup;

public sealed class DeskSetupValidationTests
{
    private static readonly DeskSetupCodePayload Sample = new()
    {
        Version = 1,
        Secret = "relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5",
        ApiUrl = "https://app.example.com",
        DeskName = "Main entrance",
    };

    private const string ExchangeSuccessBody =
        """
        {
          "secret": "relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5",
          "api_url": "https://app.example.com",
          "desk_name": "Main entrance",
          "desk_id": "44444444-4444-4444-8444-444444444444"
        }
        """;

    [Fact]
    public async Task ValidateAsync_delegates_to_legacy_path_for_desk_prefix()
    {
        var setupCode = DeskSetupCodeCodec.Encode(Sample);
        using var http = CreateRoutingHttpClient(
            exchangeStatus: System.Net.HttpStatusCode.OK,
            exchangeBody: ExchangeSuccessBody,
            pendingStatus: System.Net.HttpStatusCode.OK,
            pendingBody: """{"jobs":[]}""");

        var result = await DeskSetupValidation.ValidateAsync(
            setupCode,
            "https://app.example.com",
            http);

        Assert.True(result.Success);
        Assert.Equal(Sample.DeskName, result.Payload?.DeskName);
        Assert.Null(result.DeskId);
    }

    [Fact]
    public async Task ValidateAsync_succeeds_for_pairing_code()
    {
        using var http = CreateRoutingHttpClient(
            exchangeStatus: System.Net.HttpStatusCode.OK,
            exchangeBody: ExchangeSuccessBody,
            pendingStatus: System.Net.HttpStatusCode.OK,
            pendingBody: """{"jobs":[]}""");

        var result = await DeskSetupValidation.ValidateAsync(
            "k7mnp2qr",
            "https://app.example.com",
            http);

        Assert.True(result.Success);
        Assert.Equal("Main entrance", result.Payload?.DeskName);
        Assert.Equal("44444444-4444-4444-8444-444444444444", result.DeskId);
    }

    [Fact]
    public async Task ValidateAsync_returns_invalid_pairing_message_for_bad_format()
    {
        using var http = new HttpClient();

        var result = await DeskSetupValidation.ValidateAsync(
            "not-valid",
            "https://app.example.com",
            http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.InvalidPairingCode, result.OperatorMessage);
    }

    [Fact]
    public async Task ValidateAsync_returns_expired_message_for_exchange_400()
    {
        using var http = CreateRoutingHttpClient(
            exchangeStatus: System.Net.HttpStatusCode.BadRequest,
            exchangeBody: """{"error":"INVALID_INPUT","message":"Invalid"}""",
            pendingStatus: System.Net.HttpStatusCode.OK,
            pendingBody: """{"jobs":[]}""");

        var result = await DeskSetupValidation.ValidateAsync(
            "K7MNP2QR",
            "https://app.example.com",
            http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.PairingCodeExpiredOrUsed, result.OperatorMessage);
    }

    [Fact]
    public async Task ValidateAsync_returns_rate_limit_message_for_exchange_429()
    {
        using var http = CreateRoutingHttpClient(
            exchangeStatus: System.Net.HttpStatusCode.TooManyRequests,
            exchangeBody: """{"error":"RATE_LIMIT_EXCEEDED","message":"Too many"}""",
            pendingStatus: System.Net.HttpStatusCode.OK,
            pendingBody: """{"jobs":[]}""");

        var result = await DeskSetupValidation.ValidateAsync(
            "K7MNP2QR",
            "https://app.example.com",
            http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.TooManyAttempts, result.OperatorMessage);
    }

    private static HttpClient CreateRoutingHttpClient(
        System.Net.HttpStatusCode exchangeStatus,
        string exchangeBody,
        System.Net.HttpStatusCode pendingStatus,
        string pendingBody)
    {
        var handler = new RoutingHttpMessageHandler(
            exchangeStatus,
            exchangeBody,
            pendingStatus,
            pendingBody);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };
    }

    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly System.Net.HttpStatusCode _exchangeStatus;
        private readonly string _exchangeBody;
        private readonly System.Net.HttpStatusCode _pendingStatus;
        private readonly string _pendingBody;

        public RoutingHttpMessageHandler(
            System.Net.HttpStatusCode exchangeStatus,
            string exchangeBody,
            System.Net.HttpStatusCode pendingStatus,
            string pendingBody)
        {
            _exchangeStatus = exchangeStatus;
            _exchangeBody = exchangeBody;
            _pendingStatus = pendingStatus;
            _pendingBody = pendingBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Contains("/api/v1/print-desks/pair", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(_exchangeStatus)
                {
                    Content = new StringContent(_exchangeBody, System.Text.Encoding.UTF8, "application/json"),
                });
            }

            if (path.Contains("/api/print-queue/pending", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(_pendingStatus)
                {
                    Content = new StringContent(_pendingBody, System.Text.Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }
}
