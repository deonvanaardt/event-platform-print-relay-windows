using EventPlatform.PrintRelay.Spike.Printing;

namespace EventPlatform.PrintRelay.Spike;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        try
        {
            return RunAsync(args).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0].ToLowerInvariant();

        return command switch
        {
            "list-printers" => ListPrinters(),
            "print-test" => await PrintTestAsync(args).ConfigureAwait(true),
            "print-html" => await PrintHtmlAsync(args).ConfigureAwait(true),
            _ => UnknownCommand(command),
        };
    }

    private static int ListPrinters()
    {
        var printers = InstalledPrinters.List();

        if (printers.Count == 0)
        {
            Console.WriteLine("No printers installed.");
            return 1;
        }

        foreach (var printer in printers)
        {
            Console.WriteLine(printer);
        }

        return 0;
    }

    private static async Task<int> PrintTestAsync(string[] args)
    {
        var printerName = GetRequiredOption(args, "--printer");
        var deskName = GetOptionalOption(args, "--desk-name") ?? "Spike desk";

        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "test-badge-cr80.html");

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException(
                $"Fixture not found: {fixturePath}",
                fixturePath);
        }

        var fixtureUri = new Uri(fixturePath).AbsoluteUri;
        var query = Uri.EscapeDataString(deskName);
        var uri = $"{fixtureUri}?desk={query}";

        using var printer = new WebView2SilentPrinter();
        await printer.PrintUriAsync(uri, printerName).ConfigureAwait(true);

        Console.WriteLine($"Printed CR80 test badge to \"{printerName}\".");
        return 0;
    }

    private static async Task<int> PrintHtmlAsync(string[] args)
    {
        var printerName = GetRequiredOption(args, "--printer");
        var filePath = GetRequiredOption(args, "--file");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"HTML file not found: {filePath}", filePath);
        }

        var html = await File.ReadAllTextAsync(filePath).ConfigureAwait(true);

        using var printer = new WebView2SilentPrinter();
        await printer.PrintHtmlAsync(html, printerName).ConfigureAwait(true);

        Console.WriteLine($"Printed HTML from \"{filePath}\" to \"{printerName}\".");
        return 0;
    }

    private static string GetRequiredOption(string[] args, string name)
    {
        var value = GetOptionalOption(args, name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required option: {name}");
        }

        return value;
    }

    private static string? GetOptionalOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            Event Platform Print Relay — WebView2 spike

            Commands:
              list-printers
              print-test --printer "<name>" [--desk-name "<name>"]
              print-html --printer "<name>" --file <path>

            Gate 3 validation (Windows VM or hardware):
              1. dotnet run --project src/EventPlatform.PrintRelay.Spike -- list-printers
              2. dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-test --printer "Microsoft Print to PDF"
              3. Confirm no system print dialog appears and output matches CR80 dimensions.

            See docs/SPIKE.md for the full checklist.
            """);
    }
}
