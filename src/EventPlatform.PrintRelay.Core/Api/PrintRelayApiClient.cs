using System.Net.Http.Json;
using System.Text.Json;

namespace EventPlatform.PrintRelay.Core.Api;

public sealed class PrintRelayApiException : Exception
{
    public int Status { get; }

    public object? Body { get; }

    public PrintRelayApiException(string message, int status, object? body = null)
        : base(message)
    {
        Status = status;
        Body = body;
    }
}

public sealed class PrintRelayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _secret;

    public PrintRelayApiClient(HttpClient http, string apiUrl, string secret)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        _baseUrl = NormalizeApiUrl(apiUrl);
    }

    public static string NormalizeApiUrl(string apiUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);
        return apiUrl.TrimEnd('/');
    }

    public async Task<PendingPrintJobsResponse> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/print-queue/pending");
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PrintRelayApiException(
                $"Pending poll failed ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                body);
        }

        PendingPrintJobsResponse? pending = null;

        if (body is JsonElement element)
        {
            pending = element.Deserialize<PendingPrintJobsResponse>(JsonOptions);
        }

        if (pending?.Jobs is null)
        {
            throw new PrintRelayApiException(
                "Pending poll returned an invalid response.",
                (int)response.StatusCode,
                body);
        }

        return pending;
    }

    public async Task<PrintQueueTerminalJob> CompleteJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/print-queue/{jobId}/complete");
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PrintRelayApiException(
                $"Complete failed for job {jobId} ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                body);
        }

        var terminal = DeserializeTerminalJob(body);

        return terminal
            ?? throw new PrintRelayApiException(
                "Complete returned an invalid response.",
                (int)response.StatusCode,
                body);
    }

    public async Task<PrintQueueTerminalJob> FailJobAsync(
        string jobId,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        if (message.Length > RelayConstants.MaxFailureMessageLength)
        {
            message = message[..RelayConstants.MaxFailureMessageLength];
        }

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/print-queue/{jobId}/failed");
        request.Content = JsonContent.Create(new FailJobRequest { Message = message });

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new PrintRelayApiException(
                $"Fail report failed for job {jobId} ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                body);
        }

        var terminal = DeserializeTerminalJob(body);

        return terminal
            ?? throw new PrintRelayApiException(
                "Fail returned an invalid response.",
                (int)response.StatusCode,
                body);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_secret}");
        return request;
    }

    private static PrintQueueTerminalJob? DeserializeTerminalJob(object? body)
    {
        if (body is JsonElement element)
        {
            return element.Deserialize<PrintQueueTerminalJob>(JsonOptions);
        }

        return null;
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
