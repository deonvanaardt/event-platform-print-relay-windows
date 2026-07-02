namespace EventPlatform.PrintRelay.Core.Diagnostics;

public sealed class CompositeRelayActivitySink : IRelayActivitySink
{
    private readonly IReadOnlyList<IRelayActivitySink> _sinks;

    public CompositeRelayActivitySink(params IRelayActivitySink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);

        if (sinks.Length == 0)
        {
            throw new ArgumentException("At least one sink is required.", nameof(sinks));
        }

        _sinks = sinks;
    }

    public void Record(RelayActivityEvent activityEvent)
    {
        ArgumentNullException.ThrowIfNull(activityEvent);

        foreach (var sink in _sinks)
        {
            sink.Record(activityEvent);
        }
    }
}
