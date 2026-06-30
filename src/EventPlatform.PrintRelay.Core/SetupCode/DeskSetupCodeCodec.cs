using System.Text;
using System.Text.Json;

namespace EventPlatform.PrintRelay.Core.SetupCode;

public sealed class DeskSetupCodeException : Exception
{
    public DeskSetupCodeException(string message) : base(message)
    {
    }
}

/// <summary>
/// Encodes and decodes DESK- setup codes (v: 1).
/// </summary>
public static class DeskSetupCodeCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    public static string Encode(DeskSetupCodePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ValidatePayload(payload);

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var base64Url = ToBase64Url(Encoding.UTF8.GetBytes(json));
        return $"{RelayConstants.SetupCodePrefix}{base64Url}";
    }

    public static DeskSetupCodePayload Decode(string setupCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(setupCode);

        var trimmed = setupCode.Trim();

        if (!trimmed.StartsWith(RelayConstants.SetupCodePrefix, StringComparison.Ordinal))
        {
            throw new DeskSetupCodeException("Setup code must start with DESK-.");
        }

        var encoded = trimmed[RelayConstants.SetupCodePrefix.Length..];

        if (encoded.Length == 0)
        {
            throw new DeskSetupCodeException("Setup code payload is empty.");
        }

        byte[] jsonBytes;

        try
        {
            jsonBytes = FromBase64Url(encoded);
        }
        catch (FormatException)
        {
            throw new DeskSetupCodeException("Setup code is not valid Base64url.");
        }

        DeskSetupCodePayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<DeskSetupCodePayload>(
                jsonBytes,
                JsonOptions);
        }
        catch (JsonException)
        {
            throw new DeskSetupCodeException("Setup code JSON is invalid.");
        }

        if (payload is null)
        {
            throw new DeskSetupCodeException("Setup code JSON is empty.");
        }

        ValidatePayload(payload);
        return payload;
    }

    private static void ValidatePayload(DeskSetupCodePayload payload)
    {
        if (payload.Version != RelayConstants.SetupCodeVersion)
        {
            throw new DeskSetupCodeException(
                $"Unsupported setup code version: {payload.Version}.");
        }

        if (string.IsNullOrWhiteSpace(payload.Secret))
        {
            throw new DeskSetupCodeException("Setup code is missing secret.");
        }

        if (!payload.Secret.StartsWith("relay_", StringComparison.Ordinal))
        {
            throw new DeskSetupCodeException("Setup code secret must start with relay_.");
        }

        if (string.IsNullOrWhiteSpace(payload.ApiUrl))
        {
            throw new DeskSetupCodeException("Setup code is missing api_url.");
        }

        if (!Uri.TryCreate(payload.ApiUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new DeskSetupCodeException("Setup code api_url must be an absolute http(s) URL.");
        }

        if (payload.ApiUrl.EndsWith('/'))
        {
            throw new DeskSetupCodeException("Setup code api_url must not have a trailing slash.");
        }

        if (string.IsNullOrWhiteSpace(payload.DeskName))
        {
            throw new DeskSetupCodeException("Setup code is missing desk_name.");
        }
    }

    private static string ToBase64Url(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string base64Url)
    {
        var padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }
}
