using EventPlatform.PrintRelay.Core.Pairing;

namespace EventPlatform.PrintRelay.Core.Tests.Pairing;

public sealed class PairingCodeFormatTests
{
    [Theory]
    [InlineData("k7mnp2qr", "K7MNP2QR")]
    [InlineData("  abcd2345  ", "ABCD2345")]
    public void Normalize_trims_and_uppercases(string input, string expected)
    {
        Assert.Equal(expected, PairingCodeFormat.Normalize(input));
    }

    [Theory]
    [InlineData("K7MNP2QR")]
    [InlineData("23456789")]
    [InlineData("ABCDEFGH")]
    public void IsValid_accepts_allowed_alphabet(string code)
    {
        Assert.True(PairingCodeFormat.IsValid(code));
    }

    [Theory]
    [InlineData("K7MNP2Q")]   // too short
    [InlineData("K7MNP2QRR")]  // too long
    [InlineData("K7MNP0QR")]   // 0 not in alphabet
    [InlineData("K7MNP1QR")]   // 1 not in alphabet
    [InlineData("K7MNPIQR")]   // I not in alphabet
    [InlineData("K7MNPLQR")]   // L not in alphabet
    [InlineData("K7MNPUQR")]   // U not in alphabet
    [InlineData("K7MNPOQR")]   // O not in alphabet
    public void IsValid_rejects_invalid_codes(string code)
    {
        Assert.False(PairingCodeFormat.IsValid(code));
    }

    [Fact]
    public void Constants_match_platform_contract()
    {
        Assert.Equal(8, PairingCodeFormat.Length);
        Assert.Equal("23456789ABCDEFGHJKMNPQRSTVWXYZ", PairingCodeFormat.Alphabet);
    }
}
