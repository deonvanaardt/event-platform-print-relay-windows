using EventPlatform.PrintRelay.Core.Polling;

namespace EventPlatform.PrintRelay.Core.Tests.Polling;

public sealed class PrintJobMessagesTests
{
    [Fact]
    public void MissingBadgeHtml_matches_prd_operator_message()
    {
        Assert.Equal(
            "A badge could not be prepared for printing — contact support.",
            PrintJobMessages.MissingBadgeHtml);
    }

    [Fact]
    public void PrinterFailure_is_operator_safe()
    {
        Assert.Contains("printer", PrintJobMessages.PrinterFailure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", PrintJobMessages.PrinterFailure, StringComparison.OrdinalIgnoreCase);
    }
}
