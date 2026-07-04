using EventPlatform.PrintRelay.Core.Logging;

namespace EventPlatform.PrintRelay.Core.Tests.Logging;

public sealed class RelayLogRetentionTests
{
    [Fact]
    public void TruncateIfOversized_does_not_truncate_when_under_limit()
    {
        var path = CreateTempLogPath();

        try
        {
            File.WriteAllText(path, new string('x', 50));

            var truncated = RelayLogRetention.TruncateIfOversized(path, 100);

            Assert.False(truncated);
            Assert.Equal(50, new FileInfo(path).Length);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TruncateIfOversized_wipes_file_when_at_limit()
    {
        var path = CreateTempLogPath();

        try
        {
            File.WriteAllText(path, new string('x', 100));

            var truncated = RelayLogRetention.TruncateIfOversized(path, 100);

            Assert.True(truncated);
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TruncateIfOversized_wipes_file_when_over_limit()
    {
        var path = CreateTempLogPath();

        try
        {
            File.WriteAllText(path, new string('x', 150));

            var truncated = RelayLogRetention.TruncateIfOversized(path, 100);

            Assert.True(truncated);
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void TruncateIfOversized_returns_false_when_file_missing()
    {
        var path = CreateTempLogPath();

        var truncated = RelayLogRetention.TruncateIfOversized(path, 100);

        Assert.False(truncated);
    }

    private static string CreateTempLogPath()
    {
        return Path.Combine(Path.GetTempPath(), $"relay-log-retention-{Guid.NewGuid():N}.log");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
