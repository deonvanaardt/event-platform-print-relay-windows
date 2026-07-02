namespace EventPlatform.PrintRelay.Core.Diagnostics;

public interface IRelayActivitySink
{
    void Record(RelayActivityEvent activityEvent);
}
