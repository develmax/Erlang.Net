namespace Erlang;

public sealed partial class ProcessRuntime
{
    private static void EstablishSpawnLink(ProcessContext source, ProcessContext target)
    {
        source.LinkIdentities[target.Self] = new ProcessLinkIdentity();
        target.LinkIdentities[source.Self] = new ProcessLinkIdentity();
        source.Links.Add(target.Self);
        target.Links.Add(source.Self);
    }

    private static void EstablishLink(ProcessContext source, ProcessContext target)
    {
        if (!ActivateCallerLink(source, target.Self))
            return;
        var signal = new ProcessLinkIdentity();
        target.PendingLinkOrigins.Add(signal, source.Self);
        target.Mailbox.SendPending(
            Nil.Value,
            () =>
        {
            target.PendingLinkOrigins.Remove(signal);
            if (!target.LinkIdentities.ContainsKey(source.Self))
            {
                target.LinkIdentities.Add(source.Self, new ProcessLinkIdentity());
                target.Links.Add(source.Self);
            }

            return null;
        }
        );
    }

    private static bool ActivateCallerLink(ProcessContext source, Pid target)
    {
        if (source.Links.Contains(target))
            return false;
        if (!source.LinkIdentities.TryGetValue(target, out var identity))
        {
            identity = new ProcessLinkIdentity();
            source.LinkIdentities.Add(target, identity);
        }
        identity.PendingUnlink = null;
        source.Links.Add(target);

        return true;
    }

    private long PriorityPermissionCount(ProcessContext owner) => (long)owner.PriorityLinks.Count + owner.PriorityPortLinks.Count +
        aliases.Values.Count(alias => alias.Priority && alias.Owner.Equals(owner.Self)) +
        monitors.Values.Count(monitor => monitor.Priority && monitor.Observer.Equals(owner.Self));
}
