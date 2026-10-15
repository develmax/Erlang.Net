using System.Numerics;

namespace Erlang.Differential;

public static class ClrMemoryObservationContracts
{
    public static bool Validate(Term outcome)
    {
        if (outcome is not TupleTerm wrapper || wrapper.Items.Count != ClrMemoryCaseNames.OutcomeCount ||
            !wrapper.Items[ClrMemoryCaseNames.OutcomeTag].Equals(Term.A(OracleOutcomeTags.Success)) ||
            wrapper.Items[ClrMemoryCaseNames.OutcomeValue] is not TupleTerm snapshots || snapshots.Items.Count != ClrMemoryCaseNames.SnapshotCount)
            return false;

        // These are two independent calls. Never compare their byte values.
        return Snapshot(snapshots.Items[ClrMemoryCaseNames.AllSnapshot]) &&
            Snapshot(snapshots.Items[ClrMemoryCaseNames.SelectedSnapshot]);
    }

    private static bool Snapshot(Term snapshot)
    {
        var values = new BigInteger[ClrMemoryCaseNames.KeyCount];
        int index = 0;
        while (snapshot is Cons cell)
        {
            if (index == values.Length || cell.Head is not TupleTerm pair || pair.Items.Count != ClrMemoryCaseNames.PairCount ||
                pair.Items[ClrMemoryCaseNames.PairKey] is not Atom key || key.Name != ClrMemoryCaseNames.OrderedKeys[index] ||
                pair.Items[ClrMemoryCaseNames.PairValue] is not Integer size || size.Value.Sign < 0)
                return false;
            values[index++] = size.Value;
            snapshot = cell.Tail;
        }
        if (snapshot is not Nil || index != values.Length)
            return false;

        return values[ClrMemoryCaseNames.TotalIndex] == values[ClrMemoryCaseNames.ProcessesIndex] + values[ClrMemoryCaseNames.SystemIndex] &&
            values[ClrMemoryCaseNames.ProcessesUsedIndex] <= values[ClrMemoryCaseNames.ProcessesIndex] &&
            values[ClrMemoryCaseNames.AtomUsedIndex] <= values[ClrMemoryCaseNames.AtomIndex] &&
            values[ClrMemoryCaseNames.SystemIndex] >= values[ClrMemoryCaseNames.AtomIndex] + values[ClrMemoryCaseNames.BinaryIndex] +
                values[ClrMemoryCaseNames.CodeIndex] + values[ClrMemoryCaseNames.EtsIndex];
    }
}
