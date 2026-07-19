using System.Text.Json;
using EventPlatform.PrintRelay.Core.Diagnostics;
using EventPlatform.PrintRelay.Core.Logging;
using EventPlatform.PrintRelay.Core.Printing;

namespace EventPlatform.PrintRelay.Core.Tests.Logging;

public sealed class RelayFileLoggerTests
{
    private const long TestMaxLogBytes = 200;

    [Fact]
    public void Constructor_truncates_pre_existing_oversized_file()
    {
        var path = CreateTempLogPath();

        try
        {
            File.WriteAllText(path, new string('x', (int)TestMaxLogBytes));

            using var logger = new RelayFileLogger(path, TestMaxLogBytes);

            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void LogInfo_truncates_before_write_when_at_size_limit()
    {
        var path = CreateTempLogPath();

        try
        {
            using var logger = new RelayFileLogger(path, TestMaxLogBytes);
            logger.LogInfo(new string('a', (int)TestMaxLogBytes));

            logger.LogInfo("after truncate");

            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Contains("Log truncated due to size limit.", lines[0]);
            Assert.Contains("after truncate", lines[1]);
            Assert.True(new FileInfo(path).Length < TestMaxLogBytes);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Record_writes_valid_json_after_truncation()
    {
        var path = CreateTempLogPath();

        try
        {
            using var logger = new RelayFileLogger(path, TestMaxLogBytes);
            logger.LogInfo(new string('b', (int)TestMaxLogBytes));

            logger.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.PollSucceeded,
                Message = "Polling… 0 jobs pending (42 ms)",
                PollLatencyMs = 42,
                PendingJobCount = 0,
            });

            var lines = File.ReadAllLines(path);
            var lastLine = lines[^1];
            using var document = JsonDocument.Parse(lastLine);

            Assert.Equal("Polling… 0 jobs pending (42 ms)", document.RootElement.GetProperty("message").GetString());
            Assert.Equal("PollSucceeded", document.RootElement.GetProperty("kind").GetString());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Record_writes_page_dimensions_on_print_completed()
    {
        var path = CreateTempLogPath();

        try
        {
            using var logger = new RelayFileLogger(path, TestMaxLogBytes * 10);

            logger.Record(new RelayActivityEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = RelayActivityKind.PrintCompleted,
                Message = "Badge printed (…1234)",
                JobId = "job-1",
                PageWidthMm = 85.6,
                PageHeightMm = 54.0,
                PageSizeSource = BadgePageSizeSource.Html,
            });

            var line = File.ReadAllLines(path)[^1];
            using var document = JsonDocument.Parse(line);

            Assert.Equal(85.6, document.RootElement.GetProperty("page_width_mm").GetDouble());
            Assert.Equal(54.0, document.RootElement.GetProperty("page_height_mm").GetDouble());
            Assert.Equal("html", document.RootElement.GetProperty("page_size_source").GetString());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void Record_throws_when_json_would_contain_secret()
    {
        var path = CreateTempLogPath();

        try
        {
            using var logger = new RelayFileLogger(path, TestMaxLogBytes);

            Assert.Throws<InvalidOperationException>(() =>
                logger.Record(new RelayActivityEvent
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Kind = RelayActivityKind.PollFailed,
                    Message = "Auth failed for relay_secret_value",
                }));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string CreateTempLogPath()
    {
        return Path.Combine(Path.GetTempPath(), $"relay-file-logger-{Guid.NewGuid():N}.log");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
