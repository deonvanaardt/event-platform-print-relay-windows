namespace EventPlatform.PrintRelay.App.Tray;

internal static class RelayDiagnosticsExporter
{
    public static string SaveExport(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EventPlatform",
            "PrintRelay",
            "logs");

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "diagnostics-export.json");
        File.WriteAllText(path, json);

        return path;
    }
}
