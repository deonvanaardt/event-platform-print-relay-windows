using EventPlatform.PrintRelay.Core.SetupCode;

namespace EventPlatform.PrintRelay.Core.Tests.SetupCode;

public sealed class DeskSetupCodeCodecTests
{
    private static readonly DeskSetupCodePayload Sample = new()
    {
        Version = 1,
        Secret = "relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5",
        ApiUrl = "https://app.example.com",
        DeskName = "Main entrance",
    };

    [Fact]
    public void Encode_starts_with_DESK_prefix()
    {
        var encoded = DeskSetupCodeCodec.Encode(Sample);
        Assert.StartsWith("DESK-", encoded);
    }

    [Fact]
    public void Roundtrip_preserves_payload()
    {
        var encoded = DeskSetupCodeCodec.Encode(Sample);
        var decoded = DeskSetupCodeCodec.Decode(encoded);

        Assert.Equal(Sample.Version, decoded.Version);
        Assert.Equal(Sample.Secret, decoded.Secret);
        Assert.Equal(Sample.ApiUrl, decoded.ApiUrl);
        Assert.Equal(Sample.DeskName, decoded.DeskName);
    }

    [Fact]
    public void Decode_rejects_missing_prefix()
    {
        Assert.Throws<DeskSetupCodeException>(
            () => DeskSetupCodeCodec.Decode("relay_not_a_setup_code"));
    }

    [Fact]
    public void Encode_rejects_trailing_slash_on_api_url()
    {
        var invalid = Sample with { ApiUrl = "https://app.example.com/" };

        Assert.Throws<DeskSetupCodeException>(() => DeskSetupCodeCodec.Encode(invalid));
    }

    [Fact]
    public void Decode_rejects_invalid_secret_prefix()
    {
        var invalid = Sample with { Secret = "emp_live_not_relay" };

        Assert.Throws<DeskSetupCodeException>(
            () => DeskSetupCodeCodec.Encode(invalid));
    }
}
