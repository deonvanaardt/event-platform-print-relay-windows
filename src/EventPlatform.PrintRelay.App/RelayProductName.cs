namespace EventPlatform.PrintRelay.App;

internal static class RelayProductName
{
    public const string DisplayName = "Kiosa Print Relay";

    public const string Publisher = "Kiosa";

    public static string Title(string suffix) => $"{DisplayName} — {suffix}";

    public static string TrayTooltip(string status) => Title(status);
}
