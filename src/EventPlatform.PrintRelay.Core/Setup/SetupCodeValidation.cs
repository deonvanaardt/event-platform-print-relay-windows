using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.SetupCode;

namespace EventPlatform.PrintRelay.Core.Setup;

public static class SetupValidationMessages
{
    public const string InvalidSetupCode = "Invalid setup code — contact your event organiser.";

    public const string CouldNotConnect =
        "Could not connect — check your internet connection and try again.";
}

public sealed record SetupValidationResult
{
    public bool Success { get; init; }

    public string? OperatorMessage { get; init; }

    public DeskSetupCodePayload? Payload { get; init; }

    public static SetupValidationResult Succeeded(DeskSetupCodePayload payload) =>
        new() { Success = true, Payload = payload };

    public static SetupValidationResult Failed(string operatorMessage) =>
        new() { Success = false, OperatorMessage = operatorMessage };
}

public static class SetupCodeValidation
{
    public static async Task<SetupValidationResult> ValidateAsync(
        string setupCode,
        HttpClient http,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(http);

        DeskSetupCodePayload payload;

        try
        {
            payload = DeskSetupCodeCodec.Decode(setupCode);
        }
        catch (DeskSetupCodeException)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.InvalidSetupCode);
        }

        var client = new PrintRelayApiClient(http, payload.ApiUrl, payload.Secret);

        try
        {
            await client.GetPendingAsync(cancellationToken).ConfigureAwait(false);
            return SetupValidationResult.Succeeded(payload);
        }
        catch (PrintRelayApiException ex) when (IsAuthError(ex))
        {
            return SetupValidationResult.Failed(SetupValidationMessages.InvalidSetupCode);
        }
        catch (PrintRelayApiException ex) when (ex.Status >= 500)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.CouldNotConnect);
        }
        catch (PrintRelayApiException)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.InvalidSetupCode);
        }
        catch (HttpRequestException)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.CouldNotConnect);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.CouldNotConnect);
        }
    }

    private static bool IsAuthError(PrintRelayApiException ex)
    {
        return ex.Status is 401 or 403;
    }
}
