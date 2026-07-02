using EventPlatform.PrintRelay.Core;
using EventPlatform.PrintRelay.Core.Logging;

namespace EventPlatform.PrintRelay.App;

internal static class RelayStartupLog
{
    private static readonly object Lock = new();

    public static void Write(string message)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EventPlatform",
                "PrintRelay",
                "logs",
                "startup.log");

            var directory = Path.GetDirectoryName(logPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (Lock)
            {
                RelayLogRetention.TruncateIfOversized(logPath, RelayConstants.MaxStartupLogBytes);

                var line = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";
                File.AppendAllText(logPath, line);
            }
        }
        catch
        {
            // Never block startup on logging.
        }
    }
}
