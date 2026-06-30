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
}
