using PdfiumPrinter;

namespace EventPlatform.PrintRelay.Spike.Printing;

internal static class PdfSpooler
{
    public static void PrintFile(string pdfPath, string printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        PdfiumNativeBootstrap.EnsureLoaded();

        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException($"PDF not found: {pdfPath}", pdfPath);
        }

        var printer = new PdfPrinter(printerName);
        printer.Print(pdfPath, copies: 1, documentName: "Event Platform test badge");
    }
}
