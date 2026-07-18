using System.Text.Json;

namespace EventPlatform.PrintRelay.Core.Settings;

public static class RelaySettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "EventPlatform", "PrintRelay", "settings.json");
    }

    public static async Task<RelaySettings?> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer
            .DeserializeAsync<RelaySettings>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task SaveAsync(
        RelaySettings settings,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer
            .SerializeAsync(stream, settings, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public static Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
