namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<string, WeakReference<RuntimeDistributionEntryIdentity>> distributionEntryIdentities = new(StringComparer.Ordinal);
    private int distributionEntryIdentityLookupCount;
    private RuntimeDistributionEntryIdentity? currentDistributionEntry = new(Term.A(ProcessNodeDefaults.Local));

    internal RuntimeDistributionEntryIdentity? TryGetDistributionEntryIdentity(Atom node)
    {
        lock (gate)
        {
            if (currentDistributionEntry is null)
                return null;

            return GetDistributionEntryIdentity(node);
        }
    }

    internal RuntimeDistributionEntryIdentity GetDistributionEntryIdentity(Atom node)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(currentDistributionEntry is null, this);
            if (++distributionEntryIdentityLookupCount >= DistributionEntryIdentityParameters.ObservationSweepInterval)
            {
                distributionEntryIdentityLookupCount = 0;
                SweepDistributionEntryIdentities();
            }
            if (currentDistributionEntry is { } current && current.Node.Equals(node))
                return current;
            if (distributionEntryIdentities.TryGetValue(node.Name, out var weak) && weak.TryGetTarget(out var identity))
                return identity;
            identity = new(node);
            distributionEntryIdentities[node.Name] = new(identity);

            return identity;
        }
    }

    private void SelectDistributionEntry(Atom node, uint? admittedCreation = null)
    {
        // The caller holds the runtime gate. Preserve an old current entry only weakly.
        if (currentDistributionEntry is { } previous)
            distributionEntryIdentities[previous.Node.Name] = new(previous);
        var selected = GetDistributionEntryIdentity(node);
        if (admittedCreation is { } creation)
            selected.Creation = creation;
        currentDistributionEntry = selected;
        SweepDistributionEntryIdentities();
    }

    internal void SweepDistributionEntryIdentities()
    {
        lock (gate)
            foreach (var pair in distributionEntryIdentities.ToArray())
                if (!pair.Value.TryGetTarget(out _))
                    distributionEntryIdentities.Remove(pair.Key);
    }

    private void CloseDistributionEntryIdentities()
    {
        // Called under the runtime gate on each disposal/halt path, before resource close.
        KnownNodes.CloseObservedIdentities();
        currentDistributionEntry = null;
        distributionEntryIdentities.Clear();
    }
}
