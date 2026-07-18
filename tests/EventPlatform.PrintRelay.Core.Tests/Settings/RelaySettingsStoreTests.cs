using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.Core.Tests.Settings;

public sealed class RelaySettingsStoreTests
{
    [Fact]
    public async Task DeleteAsync_removes_existing_file()
    {
        var path = CreateTempSettingsPath();

        try
        {
            await RelaySettingsStore.SaveAsync(CreateSampleSettings(), path);

            await RelaySettingsStore.DeleteAsync(path);

            Assert.False(File.Exists(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task DeleteAsync_is_noop_when_file_missing()
    {
        var path = CreateTempSettingsPath();

        await RelaySettingsStore.DeleteAsync(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task DeleteAsync_leaves_settings_incomplete_on_reload()
    {
        var path = CreateTempSettingsPath();

        try
        {
            await RelaySettingsStore.SaveAsync(CreateSampleSettings(), path);
            await RelaySettingsStore.DeleteAsync(path);

            var settings = await RelaySettingsStore.LoadAsync(path);

            Assert.Null(settings);
            Assert.False(settings.IsComplete());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static RelaySettings CreateSampleSettings() =>
        new()
        {
            Secret = "relay_secret",
            ApiUrl = "https://example.test",
            DeskName = "Desk A",
            PrinterName = "Test Printer",
        };

    private static string CreateTempSettingsPath() =>
        Path.Combine(Path.GetTempPath(), $"relay-settings-{Guid.NewGuid():N}.json");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
