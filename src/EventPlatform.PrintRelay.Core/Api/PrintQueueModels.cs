using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventPlatform.PrintRelay.Core.Api;

public sealed record PendingPrintJobsResponse
{
    [JsonPropertyName("jobs")]
    public required IReadOnlyList<PrintQueuePendingJob> Jobs { get; init; }
}

public record PrintQueueJobResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("desk_id")]
    public required string DeskId { get; init; }

    [JsonPropertyName("event_id")]
    public required string EventId { get; init; }

    [JsonPropertyName("registration_id")]
    public required string RegistrationId { get; init; }

    [JsonPropertyName("idempotency_key")]
    public required string IdempotencyKey { get; init; }

    [JsonPropertyName("is_reprint")]
    public required bool IsReprint { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }
}

public sealed record PrintQueuePendingJob : PrintQueueJobResponse
{
    [JsonPropertyName("badge_document")]
    public JsonElement? BadgeDocument { get; init; }

    [JsonPropertyName("badge_html")]
    public string? BadgeHtml { get; init; }
}

public sealed record PrintQueueTerminalJob : PrintQueueJobResponse
{
    [JsonPropertyName("printed_at")]
    public string? PrintedAt { get; init; }

    [JsonPropertyName("failure_message")]
    public string? FailureMessage { get; init; }
}

public sealed record FailJobRequest
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
