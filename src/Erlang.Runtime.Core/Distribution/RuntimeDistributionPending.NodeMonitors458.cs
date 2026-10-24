namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    private readonly List<RuntimeNodeRegistration458> nodeMonitors458 = [];

    internal bool TryMonitorNode458(
        ProcessContext owner,
        Atom node,
        bool enable,
        bool passive
    )
    {
        lock (gate)
        {
            if (!enable)
            {
                var prior = nodeMonitors458.LastOrDefault(item => ReferenceEquals(item.Owner, owner) && item.Entry.Node.Equals(node));
                if (prior is not null && --prior.Count == 0) nodeMonitors458.Remove(prior);

                return true;
            }
            if (closed || !runtime.IsOwnedLiveNodeProcess(owner)
                || !entries.TryGetValue(node.Name, out var weak) || !weak.TryGetTarget(out var entry)) return false;
            bool connected = entry.Connected is { } channel && IsConnectedSignalGeneration(entry, channel, entry.ConnectionId);
            if (!connected && (passive || entry.State != DistributionPendingState.Pending)) return false;
            var registration = nodeMonitors458.FirstOrDefault(item => ReferenceEquals(item.Owner, owner)
                && ReferenceEquals(item.Entry, entry) && item.Generation == entry.ConnectionId);
            if (registration is null) nodeMonitors458.Add(new(owner, entry));
            else registration.Count++;

            return true;
        }
    }

    private void RetireNodeMonitors458(RuntimeDistributionPendingEntry entry)
    {
        foreach (var registration in nodeMonitors458.Where(item => ReferenceEquals(item.Entry, entry)
            && item.Generation == entry.ConnectionId).ToArray())
        {
            nodeMonitors458.Remove(registration);
            if (!runtime.IsOwnedLiveNodeProcess(registration.Owner)) continue;
            Term message = Term.Tuple(Term.A(RuntimeNodeControlNames.NodeDown), entry.Node);
            for (System.Numerics.BigInteger remaining = registration.Count; remaining > 0; remaining--)
                runtime.SendSystem(registration.Owner.Self, message);
        }
    }
}
