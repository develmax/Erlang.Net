namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void SendConnectedAliasMessage(
        ReferenceTerm reference,
        Term payload,
        Term rawToken,
        Term sender,
        bool priority
    )
    {
        lock (gate)
        {
            if (!IsCurrentLocal(reference) || !aliases.TryGetValue(reference, out var alias) ||
                !processes.TryGetValue(alias.Owner, out var target))
                return;
            target.Mailbox.SendPending(
                payload,
                () =>
            {
                lock (gate)
                {
                    if (!aliases.TryGetValue(reference, out var current))
                        return null;
                    if (current.Reply)
                    {
                        aliases.Remove(reference);
                        RemoveMonitor641(reference);
                        DetachConnectedSpawnMonitor(target, reference);
                    }

                    return priority && current.Priority;
                }
            },
                from: sender,
                distributionTraceToken: rawToken
            );
        }
    }
}
