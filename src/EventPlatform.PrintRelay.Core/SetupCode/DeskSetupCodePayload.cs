using System.Text.Json.Serialization;

namespace EventPlatform.PrintRelay.Core.SetupCode;

/// <summary>
/// Desk setup code payload (v: 1). Cross-repo contract with Event Platform admin UI.
/// </summary>
public sealed record DeskSetupCodePayload
{
    [JsonPropertyName("v")]
    public required int Version { get; init; }

    [JsonPropertyName("secret")]
    public required string Secret { get; init; }

    [JsonPropertyName("api_url")]
    public required string ApiUrl { get; init; }

    [JsonPropertyName("desk_name")]
    public required string DeskName { get; init; }
}
