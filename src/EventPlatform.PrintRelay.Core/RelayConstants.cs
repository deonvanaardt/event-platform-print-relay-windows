namespace EventPlatform.PrintRelay.Core;

/// <summary>
/// Shared constants aligned with the Event Platform print relay API.
/// </summary>
public static class RelayConstants
{
    public const int PollIntervalMs = 1000;
    public const int MaxFailureMessageLength = 500;
    public const string SetupCodePrefix = "DESK-";
    public const int SetupCodeVersion = 1;

    /// <summary>CR80 badge width (platform <c>cr80</c> preset).</summary>
    public const double Cr80WidthMm = 85.6;

    /// <summary>CR80 badge height (platform <c>cr80</c> preset).</summary>
    public const double Cr80HeightMm = 54.0;

    public const double Cr80WidthInches = Cr80WidthMm / 25.4;

    public const double Cr80HeightInches = Cr80HeightMm / 25.4;
}
