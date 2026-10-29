namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    private readonly Dictionary<BinaryProgramBacking598, ReferenceResourceEntry> binaryEntries598 = new(ReferenceEqualityComparer.Instance);

    internal ResourceReferenceTerm CreateBinarySearchPattern(BinarySearchPattern pattern)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, BinaryArenaNames598.Closed);
            if (binaryEntries598.TryGetValue(pattern.Backing598, out var previous)) return new ResourceReferenceTerm(previous);
            binaryEntries598.EnsureCapacity(checked(binaryEntries598.Count + 1));
            var reference = Create(ReferenceResourceKind.BinarySearchPattern, pattern);
            try
            {
                pattern.Backing598.BindIdentity598(reference.Words);
                binaryEntries598.Add(pattern.Backing598, reference.Entry);

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

    internal ReferenceResourceLease<BinarySearchPattern>? AcquireBinarySearchPattern(ReferenceTerm reference)
        => Acquire<BinarySearchPattern>(reference, ReferenceResourceKind.BinarySearchPattern);

    internal void ForgetBinaryProgram598(BinaryProgramBacking598 backing)
    {
        lock (gate)
        {
            if (!binaryEntries598.Remove(backing, out var entry)) return;
            entries.Remove(entry.Identity);
            entry.Close();
        }
    }
}
