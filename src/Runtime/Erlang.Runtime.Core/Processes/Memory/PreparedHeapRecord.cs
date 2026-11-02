namespace Erlang;

internal sealed class PreparedHeapRecord
{
    internal NativeRecordTerm Source { get; }
    internal TupleTerm Definition { get; }
    internal Term[] Children { get; }

    internal PreparedHeapRecord(NativeRecordTerm source)
    {
        Source = source;
        Definition = NativeRecordHash.LegacyDefinition(source);
        int[] indexes = NativeRecordHash.SortedIndexes(source).ToArray();
        Children = new Term[indexes.Length + 1];
        Children[0] = Definition;
        for (int index = 0; index < indexes.Length; index++) Children[index + 1] = source.Values[indexes[index]];
    }

    // Private generated metadata contains only actual definition/order tuples,
    // atoms and immediate integers. It introduces no resource/code capability.
    internal void AdmitMetadata(ProcessTermConstruction514 construction)
    {
        var pending = new Stack<Term>();
        pending.Push(Definition);
        while (pending.TryPop(out Term? value))
        {
            if (!construction.Admit(value)) continue;
            if (value is TupleTerm tuple)
            {
                foreach (Term child in tuple.Items) pending.Push(child);
            }
            else if (value is not Atom && !ProcessTermHeap506.IsImmediateInteger559(value))
            {
                throw new InvalidOperationException(NativeRecordHeapLayout.InvalidMetadata);
            }
        }
    }
}
