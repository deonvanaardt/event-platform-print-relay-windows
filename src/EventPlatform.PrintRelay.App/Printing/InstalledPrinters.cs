using System.Drawing.Printing;

namespace EventPlatform.PrintRelay.App.Printing;

public static class InstalledPrinters
{
    public static IReadOnlyList<string> List()
    {
        var printers = new List<string>();

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }

        printers.Sort(StringComparer.OrdinalIgnoreCase);
        return printers;
    }

    public static bool Exists(string printerName)
    {
        return List().Contains(printerName, StringComparer.OrdinalIgnoreCase);
    }
}
