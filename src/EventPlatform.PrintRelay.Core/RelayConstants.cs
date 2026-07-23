namespace EventPlatform.PrintRelay.Core;

/// <summary>
/// Shared constants aligned with the Event Platform print relay API.
/// </summary>
public static class RelayConstants
{
    public const int PollIntervalMs = 1000;
    public const int MaxFailureMessageLength = 500;

    /// <summary>Max size of <c>relay.log</c> before in-place wipe (5 MB).</summary>
    public const long MaxRelayLogBytes = 5 * 1024 * 1024;

    /// <summary>Max size of <c>startup.log</c> before in-place wipe (256 KB).</summary>
    public const long MaxStartupLogBytes = 256 * 1024;
    public const string SetupCodePrefix = "DESK-";
    public const int SetupCodeVersion = 1;

    /// <summary>Default Kiosa platform origin for pairing exchange (production).</summary>
    public const string DefaultPlatformUrl = "https://app.kiosa.io";

    /// <summary>CR80 badge width (platform <c>cr80</c> preset).</summary>
    public const double Cr80WidthMm = 85.6;

    /// <summary>CR80 badge height (platform <c>cr80</c> preset).</summary>
    public const double Cr80HeightMm = 54.0;

    public const double Cr80WidthInches = Cr80WidthMm / 25.4;

    public const double Cr80HeightInches = Cr80HeightMm / 25.4;

    /// <summary>ISO A5 width (spike testing on common office paper).</summary>
    public const double A5WidthMm = 148.0;

    /// <summary>ISO A5 height (spike testing on common office paper).</summary>
    public const double A5HeightMm = 210.0;

    public const double A5WidthInches = A5WidthMm / 25.4;

    public const double A5HeightInches = A5HeightMm / 25.4;
}
