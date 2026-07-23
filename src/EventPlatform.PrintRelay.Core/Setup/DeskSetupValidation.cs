using EventPlatform.PrintRelay.Core.Api;
using EventPlatform.PrintRelay.Core.Pairing;
using EventPlatform.PrintRelay.Core.SetupCode;

namespace EventPlatform.PrintRelay.Core.Setup;

public static class DeskSetupValidation
{
    public static async Task<SetupValidationResult> ValidateAsync(
        string input,
        string platformBaseUrl,
        HttpClient http,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformBaseUrl);

        var trimmed = input.Trim();

        if (trimmed.StartsWith(RelayConstants.SetupCodePrefix, StringComparison.Ordinal))
        {
            return await SetupCodeValidation.ValidateAsync(trimmed, http, cancellationToken)
                .ConfigureAwait(false);
        }

        var normalized = PairingCodeFormat.Normalize(trimmed);

        if (!PairingCodeFormat.IsValid(normalized))
        {
            return SetupValidationResult.Failed(SetupValidationMessages.InvalidPairingCode);
        }

        var exchangeClient = new PairingExchangeClient(http, platformBaseUrl);

        PairingExchangeResult exchange;

        try
        {
            exchange = await exchangeClient.ExchangeAsync(normalized, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (PrintRelayApiException ex) when (ex.Status is 400)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.PairingCodeExpiredOrUsed);
        }
        catch (PrintRelayApiException ex) when (ex.Status is 429)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.TooManyAttempts);
        }
        catch (PrintRelayApiException ex) when (ex.Status >= 500)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.CouldNotConnect);
        }
        catch (PrintRelayApiException)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.PairingCodeExpiredOrUsed);
        }
        catch (HttpRequestException)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.CouldNotConnect);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SetupValidationResult.Failed(SetupValidationMessages.CouldNotConnect);
        }

        var payload = new DeskSetupCodePayload
        {
            Version = RelayConstants.SetupCodeVersion,
            Secret = exchange.Secret,
            ApiUrl = exchange.ApiUrl,
            DeskName = exchange.DeskName,
        };

        var client = new PrintRelayApiClient(http, payload.ApiUrl, payload.Secret);

        try
        {
            await client.GetPendingAsync(cancellationToken).ConfigureAwait(false);
            return SetupValidationResult.Succeeded(payload, exchange.DeskId);
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
