namespace EventPlatform.PrintRelay.Core.Logging;

/// <summary>
/// Size-based in-place log truncation. Never throws — logging must not break the relay.
/// </summary>
public static class RelayLogRetention
{
    /// <summary>
    /// Wipes <paramref name="filePath"/> when its length is at or above <paramref name="maxBytes"/>.
    /// </summary>
    /// <returns><c>true</c> if the file was truncated.</returns>
    public static bool TruncateIfOversized(string filePath, long maxBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (maxBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes), "Max bytes must be positive.");
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var length = new FileInfo(filePath).Length;

            if (length < maxBytes)
            {
                return false;
            }

            File.WriteAllText(filePath, string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
