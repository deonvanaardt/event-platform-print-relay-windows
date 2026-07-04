namespace EventPlatform.PrintRelay.Core.Diagnostics;

public sealed record RelayJobSummary
{
    public required string JobId { get; init; }

    public required string DeskId { get; init; }

    public required string EventId { get; init; }

    public required string RegistrationId { get; init; }

    public required DateTimeOffset ReceivedAtUtc { get; init; }

    public string? Outcome { get; init; }

    public string? FailureMessage { get; init; }
}
