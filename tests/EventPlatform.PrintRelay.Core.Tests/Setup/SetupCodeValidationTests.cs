using EventPlatform.PrintRelay.Core.Setup;
using EventPlatform.PrintRelay.Core.SetupCode;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.Core.Tests.Setup;

public sealed class SetupCodeValidationTests
{
    private static readonly DeskSetupCodePayload Sample = new()
    {
        Version = 1,
        Secret = "relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5",
        ApiUrl = "https://app.example.com",
        DeskName = "Main entrance",
    };

    [Fact]
    public async Task ValidateAsync_succeeds_on_empty_pending_response()
    {
        var setupCode = DeskSetupCodeCodec.Encode(Sample);
        using var http = CreateHttpClient("""{"jobs":[]}""", System.Net.HttpStatusCode.OK);

        var result = await SetupCodeValidation.ValidateAsync(setupCode, http);

        Assert.True(result.Success);
        Assert.Equal(Sample.DeskName, result.Payload?.DeskName);
    }

    [Fact]
    public async Task ValidateAsync_returns_invalid_message_for_malformed_code()
    {
        using var http = new HttpClient();

        var result = await SetupCodeValidation.ValidateAsync("not-a-setup-code", http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.InvalidSetupCode, result.OperatorMessage);
    }

    [Fact]
    public async Task ValidateAsync_returns_invalid_message_for_401()
    {
        var setupCode = DeskSetupCodeCodec.Encode(Sample);
        using var http = CreateHttpClient("""{"error":"Unauthorized"}""", System.Net.HttpStatusCode.Unauthorized);

        var result = await SetupCodeValidation.ValidateAsync(setupCode, http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.InvalidSetupCode, result.OperatorMessage);
    }

    [Fact]
    public async Task ValidateAsync_returns_invalid_message_for_403()
    {
        var setupCode = DeskSetupCodeCodec.Encode(Sample);
        using var http = CreateHttpClient("""{"error":"Forbidden"}""", System.Net.HttpStatusCode.Forbidden);

        var result = await SetupCodeValidation.ValidateAsync(setupCode, http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.InvalidSetupCode, result.OperatorMessage);
    }

    [Fact]
    public async Task ValidateAsync_returns_connect_message_for_500()
    {
        var setupCode = DeskSetupCodeCodec.Encode(Sample);
        using var http = CreateHttpClient("""{"error":"Server error"}""", System.Net.HttpStatusCode.InternalServerError);

        var result = await SetupCodeValidation.ValidateAsync(setupCode, http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.CouldNotConnect, result.OperatorMessage);
    }

    [Fact]
    public async Task ValidateAsync_returns_connect_message_for_network_failure()
    {
        var setupCode = DeskSetupCodeCodec.Encode(Sample);
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Network unreachable."));
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };

        var result = await SetupCodeValidation.ValidateAsync(setupCode, http);

        Assert.False(result.Success);
        Assert.Equal(SetupValidationMessages.CouldNotConnect, result.OperatorMessage);
    }

    private static HttpClient CreateHttpClient(string responseBody, System.Net.HttpStatusCode statusCode)
    {
        var handler = new StatusHttpMessageHandler(responseBody, statusCode);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.example.com"),
        };
    }

    private sealed class StatusHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly System.Net.HttpStatusCode _statusCode;

        public StatusHttpMessageHandler(string responseBody, System.Net.HttpStatusCode statusCode)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(_exception);
        }
    }
}

public sealed class RelaySettingsExtensionsTests
{
    [Fact]
    public void IsComplete_returns_false_for_null()
    {
        Assert.False(((RelaySettings?)null).IsComplete());
    }

    [Fact]
    public void IsComplete_returns_false_when_printer_missing()
    {
        var settings = new RelaySettings
        {
            Secret = "relay_test",
            ApiUrl = "https://app.example.com",
            DeskName = "Main entrance",
            PrinterName = "",
        };

        Assert.False(settings.IsComplete());
    }

    [Fact]
    public void IsComplete_returns_true_when_all_fields_present()
    {
        var settings = new RelaySettings
        {
            Secret = "relay_test",
            ApiUrl = "https://app.example.com",
            DeskName = "Main entrance",
            PrinterName = "Microsoft Print to PDF",
        };

        Assert.True(settings.IsComplete());
    }
}

public sealed class RelaySettingsStoreTests
{
    [Fact]
    public async Task Save_and_load_roundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"relay-settings-{Guid.NewGuid():N}.json");
        var settings = new RelaySettings
        {
            Secret = "relay_test",
            ApiUrl = "https://app.example.com",
            DeskName = "Main entrance",
            PrinterName = "Microsoft Print to PDF",
        };

        try
        {
            await RelaySettingsStore.SaveAsync(settings, path);
            var loaded = await RelaySettingsStore.LoadAsync(path);

            Assert.NotNull(loaded);
            Assert.Equal(settings.Secret, loaded.Secret);
            Assert.Equal(settings.ApiUrl, loaded.ApiUrl);
            Assert.Equal(settings.DeskName, loaded.DeskName);
            Assert.Equal(settings.PrinterName, loaded.PrinterName);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
