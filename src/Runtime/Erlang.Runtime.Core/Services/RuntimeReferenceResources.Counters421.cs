namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    // Identity index only. Entries own no backing root/host carrier.
    private readonly Dictionary<CountersArray421, ReferenceResourceEntry> countersEntries596 = new(ReferenceEqualityComparer.Instance);

    internal ResourceReferenceTerm CreateCounters421(CountersGuestRoot421 root)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, CountersOwnership596Names.Retired);
            if (countersEntries596.TryGetValue(root.Array, out ReferenceResourceEntry? known))
                return new ResourceReferenceTerm(known);
            countersEntries596.EnsureCapacity(checked(countersEntries596.Count + 1));
            ResourceReferenceTerm reference = Create(ReferenceResourceKind.Counters421, root);
            try
            {
                root.Array.BindIdentity(reference.Words);
                countersEntries596.Add(root.Array, reference.Entry);

                return reference;
            }
            catch
            {
                entries.Remove(reference.Entry.Identity);
                reference.Entry.Close();
                throw;
            }
        }
    }

    internal CountersResourceLease596? AcquireCounters421(ReferenceTerm reference)
    {
        ReferenceResourceLease<CountersGuestRoot421>? acquired = Acquire<CountersGuestRoot421>(reference, ReferenceResourceKind.Counters421);
        if (acquired is null) return null;
        try
        {
            return new(acquired, reference);
        }
        catch
        {
            acquired.Dispose();
            throw;
        }
    }

    // Parent owner calls only AFTER actual array physical retirement, outside
    // its own gate. Closing a registry entry must never force payload freeing.
    internal void ForgetCounters596(CountersArray421 array)
    {
        lock (gate)
        {
            if (!countersEntries596.Remove(array, out ReferenceResourceEntry? entry)) return;
            entries.Remove(entry.Identity);
            entry.Close();
        }
    }
}
