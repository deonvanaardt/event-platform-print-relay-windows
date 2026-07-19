using EventPlatform.PrintRelay.Core.Printing;

namespace EventPlatform.PrintRelay.Core.Polling;

public sealed record PrintJobOutcome(
    bool Succeeded,
    string? FailureMessage = null,
    BadgePageDimensions? PageDimensions = null)
{
    public static PrintJobOutcome Success() => new(true);

    public static PrintJobOutcome Success(BadgePageDimensions dimensions) =>
        new(true, PageDimensions: dimensions);

    public static PrintJobOutcome Failure(string message) => new(false, message);
}
