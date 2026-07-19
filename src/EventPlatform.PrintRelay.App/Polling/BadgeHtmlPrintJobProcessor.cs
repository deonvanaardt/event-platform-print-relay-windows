using EventPlatform.PrintRelay.App.Printing;
using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Polling;
using EventPlatform.PrintRelay.Core.Printing;

namespace EventPlatform.PrintRelay.App.Polling;

public sealed class BadgeHtmlPrintJobProcessor : IPrintJobProcessor
{
    private readonly string _printerName;
    private readonly WebView2SilentPrinter _printer;

    public BadgeHtmlPrintJobProcessor(string printerName, WebView2SilentPrinter printer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
        _printerName = printerName;
        _printer = printer ?? throw new ArgumentNullException(nameof(printer));
    }

    public async Task<PrintJobOutcome> ProcessAsync(
        PrintQueuePendingJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (string.IsNullOrWhiteSpace(job.BadgeHtml))
        {
            return PrintJobOutcome.Failure(PrintJobMessages.MissingBadgeHtml);
        }

        try
        {
            var dimensions = BadgePageDimensionResolver.Resolve(job.BadgeHtml, job.BadgeDocument);

            await _printer
                .PrintHtmlAsync(job.BadgeHtml, _printerName, dimensions, cancellationToken)
                .ConfigureAwait(false);

            return PrintJobOutcome.Success(dimensions);
        }
        catch (Exception)
        {
            return PrintJobOutcome.Failure(PrintJobMessages.PrinterFailure);
        }
    }
}
