namespace EventPlatform.PrintRelay.App.Printing;

internal static class WebView2Paths
{
    /// <summary>
    /// Writable user-data folder for WebView2. Required when the app runs from
    /// Program Files (MSI install) — the default next-to-exe folder is not writable.
    /// </summary>
    public static string UserDataFolder
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EventPlatform",
                "PrintRelay",
                "WebView2");

            Directory.CreateDirectory(path);
            return path;
        }
    }
}
