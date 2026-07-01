namespace EventPlatform.PrintRelay.Core.Settings;

public static class RelaySettingsExtensions
{
    public static bool IsComplete(this RelaySettings? settings)
    {
        if (settings is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(settings.Secret)
            && !string.IsNullOrWhiteSpace(settings.ApiUrl)
            && !string.IsNullOrWhiteSpace(settings.DeskName)
            && !string.IsNullOrWhiteSpace(settings.PrinterName);
    }
}
