using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Printing;

namespace EventPlatform.PrintRelay.Core.Diagnostics;

public sealed record RelayActivityEvent
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required RelayActivityKind Kind { get; init; }

    public required string Message { get; init; }

    public string? JobId { get; init; }

    public string? DeskId { get; init; }

    public string? EventId { get; init; }

    public string? RegistrationId { get; init; }

    public int? PollLatencyMs { get; init; }

    public int? PendingJobCount { get; init; }

    public PrintRelayPollConnectionState? ConnectionState { get; init; }

    public bool BadgeHtmlPresent { get; init; }

    public string? FailureMessage { get; init; }

    public double? PageWidthMm { get; init; }

    public double? PageHeightMm { get; init; }

    public BadgePageSizeSource? PageSizeSource { get; init; }
}
