namespace Erlang;

internal sealed partial class ProcessTracing
{
    internal bool CanPublishDirectMailboxBacking()
    {
        lock (gate)
            return settings.Count == 0 && namedSessions425.All(session => !session.Alive);
    }
}
