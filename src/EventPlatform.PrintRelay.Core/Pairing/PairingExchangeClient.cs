using System.Net.Http.Json;
using System.Text.Json;
using EventPlatform.PrintRelay.Core.Api;

namespace EventPlatform.PrintRelay.Core.Pairing;

public sealed class PairingExchangeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly string _platformBaseUrl;

    public PairingExchangeClient(HttpClient http, string platformBaseUrl)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _platformBaseUrl = PrintRelayApiClient.NormalizeApiUrl(platformBaseUrl);
    }

    public async Task<PairingExchangeResult> ExchangeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_platformBaseUrl}/api/v1/print-desks/pair");
        request.Content = JsonContent.Create(new { code });

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PrintRelayApiException(
                $"Pairing exchange failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                body);
        }

        PairingExchangeResult? result = null;

        if (body is JsonElement element)
        {
            result = element.Deserialize<PairingExchangeResult>(JsonOptions);
        }

        if (result is null
            || string.IsNullOrWhiteSpace(result.Secret)
            || !result.Secret.StartsWith("relay_", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(result.ApiUrl)
            || string.IsNullOrWhiteSpace(result.DeskName)
            || string.IsNullOrWhiteSpace(result.DeskId))
        {
            throw new PrintRelayApiException(
                "Pairing exchange returned an invalid response.",
                (int)response.StatusCode,
                body);
        }

        return result;
    }

    private static async Task<object?> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(text);
        }
        catch (JsonException)
        {
            return text;
        }
    }
}
