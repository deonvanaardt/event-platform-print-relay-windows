namespace EventPlatform.PrintRelay.Core.Polling;

public sealed record PrintJobOutcome(bool Succeeded, string? FailureMessage = null)
{
    public static PrintJobOutcome Success() => new(true);

    public static PrintJobOutcome Failure(string message) => new(false, message);
}
