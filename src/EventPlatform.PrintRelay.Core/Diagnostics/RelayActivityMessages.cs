using EventPlatform.PrintRelay.Core.Polling;

namespace EventPlatform.PrintRelay.Core.Diagnostics;

public static class RelayActivityMessages
{
    public static string PollSucceeded(int pendingJobCount, int latencyMs) =>
        $"Polling… {pendingJobCount} job{(pendingJobCount == 1 ? "" : "s")} pending ({latencyMs} ms)";

    public static string PollFailed(string reason) =>
        $"Poll failed — {reason}";

    public static string JobReceived(string registrationIdSuffix) =>
        $"Received job for registration …{registrationIdSuffix}";

    public static string PrintStarted(string jobIdSuffix) =>
        $"Printing job …{jobIdSuffix}";

    public static string PrintCompleted(string jobIdSuffix) =>
        $"Printed job …{jobIdSuffix}";

    public static string PrintFailed(string jobIdSuffix, string reason) =>
        $"Failed to print job …{jobIdSuffix} — {reason}";

    public static string JobAcknowledged(string jobIdSuffix, string outcome) =>
        $"Job …{jobIdSuffix} marked {outcome}";

    public static string ConnectionState(PrintRelayPollConnectionState state) =>
        state switch
        {
            PrintRelayPollConnectionState.Connected => "Connected to platform",
            PrintRelayPollConnectionState.BackingOff => "Reconnecting to platform…",
            PrintRelayPollConnectionState.AuthError => "Setup code is no longer valid",
            _ => state.ToString(),
        };

    public static string ManualConnectionTest(int pendingJobCount, int latencyMs) =>
        $"Connection test OK — {pendingJobCount} job{(pendingJobCount == 1 ? "" : "s")} pending ({latencyMs} ms)";

    public static string IdSuffix(string? value, int length = 8)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Length <= length ? value : value[^length..];
    }
}
