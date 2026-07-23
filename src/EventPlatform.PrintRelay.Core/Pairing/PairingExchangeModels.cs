using System.Text.Json.Serialization;

namespace EventPlatform.PrintRelay.Core.Pairing;

public sealed record PairingExchangeResult
{
    [JsonPropertyName("secret")]
    public required string Secret { get; init; }

    [JsonPropertyName("api_url")]
    public required string ApiUrl { get; init; }

    [JsonPropertyName("desk_name")]
    public required string DeskName { get; init; }

    [JsonPropertyName("desk_id")]
    public required string DeskId { get; init; }
}
