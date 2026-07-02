namespace EventPlatform.PrintRelay.Core.Diagnostics;

public enum RelayActivityKind
{
    PollSucceeded,
    PollFailed,
    JobReceived,
    PrintStarted,
    PrintCompleted,
    PrintFailed,
    JobAcknowledged,
    ConnectionStateChanged,
    ManualConnectionTest,
    RelayStopped,
}
