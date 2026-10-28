namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    // Identity index only. Entries own no backing root/host carrier.
    private readonly Dictionary<AtomicsArray413, ReferenceResourceEntry> atomicsEntries586 = new(ReferenceEqualityComparer.Instance);

    internal ResourceReferenceTerm CreateAtomics413(AtomicsGuestRoot413 root)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, AtomicsOwnership586Names.Retired);
            if (atomicsEntries586.TryGetValue(root.Array, out ReferenceResourceEntry? known))
                return new ResourceReferenceTerm(known);
            atomicsEntries586.EnsureCapacity(checked(atomicsEntries586.Count + 1));
            ResourceReferenceTerm reference = Create(ReferenceResourceKind.Atomics413, root);
            try
            {
                root.Array.BindIdentity(reference.Words);
                atomicsEntries586.Add(root.Array, reference.Entry);

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

    internal AtomicsResourceLease586? AcquireAtomics413(ReferenceTerm reference)
    {
        ReferenceResourceLease<AtomicsGuestRoot413>? acquired = Acquire<AtomicsGuestRoot413>(reference, ReferenceResourceKind.Atomics413);
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
    internal void ForgetAtomics586(AtomicsArray413 array)
    {
        lock (gate)
        {
            if (!atomicsEntries586.Remove(array, out ReferenceResourceEntry? entry)) return;
            entries.Remove(entry.Identity);
            entry.Close();
        }
    }
}
