using System.Text.RegularExpressions;

namespace EventPlatform.PrintRelay.Core.Pairing;

public static partial class PairingCodeFormat
{
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const int Length = 8;

    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Trim().ToUpperInvariant();
    }

    public static bool IsValid(string normalized)
    {
        return PairingCodePattern().IsMatch(normalized);
    }

    [GeneratedRegex("^[23456789ABCDEFGHJKMNPQRSTVWXYZ]{8}$")]
    private static partial Regex PairingCodePattern();
}
