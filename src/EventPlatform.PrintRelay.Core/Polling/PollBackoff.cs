namespace EventPlatform.PrintRelay.Core.Polling;

/// <summary>
/// Exponential backoff for API connectivity failures (PRD §8.1).
/// </summary>
public sealed class PollBackoff
{
    private static readonly int[] StepsMs = [2000, 4000, 8000, 16000];
    private const int CapMs = 60_000;

    private int _stepIndex;

    public int CurrentDelayMs { get; private set; } = RelayConstants.PollIntervalMs;

    public int NextDelayMs()
    {
        if (_stepIndex >= StepsMs.Length)
        {
            CurrentDelayMs = CapMs;
            return CapMs;
        }

        CurrentDelayMs = StepsMs[_stepIndex];
        _stepIndex++;
        return CurrentDelayMs;
    }

    public void Reset()
    {
        _stepIndex = 0;
        CurrentDelayMs = RelayConstants.PollIntervalMs;
    }
}
