namespace Erlang;

internal static class ProcessResourcePolicyParser508
{
    // This is a resource-option component, not a replacement full spawn parser.
    // False means the existing option parser must validate that option itself.
    internal static bool TryApplySpawnOption(
        PinnedHeapSizeClasses508 classes,
        ProcessResourcePolicy508 defaults,
        ProcessResourcePolicy508 current,
        Term option,
        out ProcessResourcePolicy508 updated
    )
    {
        updated = current;
        if (option is not TupleTerm { Items: [Atom name, var value] })
            return false;
        switch (name.Name)
        {
            case ResourcePolicy508Names.MinimumHeap:
                updated = current with { MinimumHeapWords = Minimum(classes, defaults.MinimumHeapWords, value) };

                return true;
            case ResourcePolicy508Names.MinimumBinaryHeap:
                updated = current with { MinimumBinaryHeapWords = Minimum(classes, defaults.MinimumBinaryHeapWords, value) };

                return true;
            case ResourcePolicy508Names.FullSweepAfter:
                updated = current with { FullSweepAfter = unchecked((ushort)classes.NonnegativeSmall(value)) };

                return true;
            default:
                return false;
        }
    }

    internal static ulong Minimum(PinnedHeapSizeClasses508 classes, ulong vmDefault, Term value)
    {
        ulong requested = classes.NonnegativeSmall(value);

        return requested < vmDefault ? vmDefault : classes.Next(requested);
    }

    internal static ushort SystemSweep(PinnedHeapSizeClasses508 classes, Term value)
    {
        ulong requested = classes.NonnegativeSmall(value);

        return requested > ushort.MaxValue ? ushort.MaxValue : (ushort)requested;
    }
}
