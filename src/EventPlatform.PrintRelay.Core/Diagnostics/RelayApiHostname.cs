namespace EventPlatform.PrintRelay.Core.Diagnostics;

public static class RelayApiHostname
{
    public static string FromApiUrl(string apiUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);

        if (!Uri.TryCreate(apiUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return apiUrl.Trim();
        }

        return uri.Host;
    }
}
