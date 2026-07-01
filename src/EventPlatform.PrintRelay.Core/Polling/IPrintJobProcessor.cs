using EventPlatform.PrintRelay.Core.Api;

namespace EventPlatform.PrintRelay.Core.Polling;

public interface IPrintJobProcessor
{
    Task<PrintJobOutcome> ProcessAsync(
        PrintQueuePendingJob job,
        CancellationToken cancellationToken = default);
}
