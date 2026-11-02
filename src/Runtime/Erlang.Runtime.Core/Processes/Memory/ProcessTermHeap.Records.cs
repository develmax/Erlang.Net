namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // This table resolves the real native definition pointer; it is NOT a GC
    // root. Only actual record/root traversal marks the pointed definition.
    private readonly Dictionary<long, TupleTerm> recordDefinitions = new();

    internal NativeRecordTerm CreateNativeRecord(
        NativeRecordTerm source,
        PreparedHeapRecord plan,
        Term[] copied,
        ProcessTermConstruction514 construction
    )
    {
        if (!Monitor.IsEntered(Gate) || !ReferenceEquals(plan.Source, source) || !construction.Contains(source)
            || copied.Length != plan.Children.Length || copied.Length == 0 || copied[0] is not TupleTerm definition)
            throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        foreach (Term value in copied)
            if (!construction.Contains(value)) throw new InvalidOperationException(ProcessTermConstruction514Names.UnadmittedChild);
        _ = IsOwnedLocal(definition);
        ProcessTermSequence506 definitionBlock = Storage(definition) as ProcessTermSequence506
            ?? throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        int count = copied.Length - 1;
        ValidateRecordDefinition(definition, count);
        nuint pointer = (nuint)CaptureRawAddress571(definitionBlock, 0, NativeRecordHeapLayout.DefinitionHeaderWords).Address571
            | NativeRecordHeapLayout.BoxedPointerTag;
        recordDefinitions.EnsureCapacity(checked(recordDefinitions.Count + 1));
        ProcessTermSequence506 block = AllocateEncoded526(
            copied[1..],
            [NativeRecordHeapLayout.Header(count)],
            [pointer]
        );
        try
        {
            recordDefinitions.Add(block.Identity, definition);
            var result = new NativeRecordTerm(block);
            construction.Publish(result);

            return result;
        }
        catch
        {
            recordDefinitions.Remove(block.Identity);
            ReleaseRegisteredHeader526(block);
            throw;
        }
    }

    private static void ValidateRecordDefinition(TupleTerm definition, int count)
    {
        if (definition.Items.Count != checked(NativeRecordHeapLayout.DefinitionKeys + count)
            || definition.Items[NativeRecordHeapLayout.DefinitionOrder] is not TupleTerm order || order.Items.Count != count
            || definition.Items[NativeRecordHeapLayout.DefinitionHash] is not Integer { Value.Sign: >= 0 }
            || definition.Items[NativeRecordHeapLayout.DefinitionModule] is not Atom
            || definition.Items[NativeRecordHeapLayout.DefinitionName] is not Atom
            || definition.Items[NativeRecordHeapLayout.DefinitionExported] is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
            throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        var positions = new HashSet<int>();
        var names = new HashSet<Atom>();
        for (int index = 0; index < count; index++)
        {
            if (order.Items[index] is not Integer position || position.Value < 0 || position.Value >= count
                || !positions.Add((int)position.Value) || definition.Items[NativeRecordHeapLayout.DefinitionKeys + index] is not Atom name
                || !names.Add(name)) throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        }
    }

    private TupleTerm RequireRecordDefinition(ProcessTermSequence506 block)
    {
        if (!ReferenceEquals(block.Owner, this) || !blocks.TryGetValue(block.Identity, out var live) || !ReferenceEquals(live, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        if (block.HeaderWords526 != NativeRecordHeapLayout.HeaderWords
            || block.RawPayloadWords556 != NativeRecordHeapLayout.DefinitionPointerWords
            || block.Slab.Cells.ReadHeader526(block.AllocationOffset526) != NativeRecordHeapLayout.Header(block.Count)
            || !recordDefinitions.TryGetValue(block.Identity, out var definition))
            throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        _ = IsOwnedLocal(definition);
        var definitionBlock = Storage(definition) as ProcessTermSequence506
            ?? throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        nuint expected = (nuint)CaptureRawAddress571(definitionBlock, 0, NativeRecordHeapLayout.DefinitionHeaderWords).Address571
            | NativeRecordHeapLayout.BoxedPointerTag;
        if (block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + NativeRecordHeapLayout.HeaderWords) != expected)
            throw new InvalidOperationException(NativeRecordHeapLayout.Invalid);
        ValidateRecordDefinition(definition, block.Count);

        return definition;
    }

    internal TupleTerm ReadRecordDefinition(ProcessTermSequence506 block)
    {
        lock (Gate) return RequireRecordDefinition(block);
    }

    internal Atom ReadRecordModule(ProcessTermSequence506 block)
    {
        lock (Gate) return (Atom)RequireRecordDefinition(block).Items[NativeRecordHeapLayout.DefinitionModule];
    }

    internal Atom ReadRecordName(ProcessTermSequence506 block)
    {
        lock (Gate) return (Atom)RequireRecordDefinition(block).Items[NativeRecordHeapLayout.DefinitionName];
    }

    internal bool ReadRecordExported(ProcessTermSequence506 block)
    {
        lock (Gate) return ((Atom)RequireRecordDefinition(block).Items[NativeRecordHeapLayout.DefinitionExported]).Name == ErlangBooleanAtoms.True;
    }

    internal IReadOnlyList<Atom> ReadRecordFieldNames(ProcessTermSequence506 block)
    {
        lock (Gate)
        {
            TupleTerm definition = RequireRecordDefinition(block);
            var order = (TupleTerm)definition.Items[NativeRecordHeapLayout.DefinitionOrder];
            var values = new Atom[block.Count];
            for (int index = 0; index < values.Length; index++)
                values[index] = (Atom)definition.Items[NativeRecordHeapLayout.DefinitionKeys + (int)((Integer)order.Items[index]).Value];

            return Array.AsReadOnly(values);
        }
    }

    internal IReadOnlyList<Term> ReadRecordValues(ProcessTermSequence506 block)
    {
        lock (Gate)
        {
            TupleTerm definition = RequireRecordDefinition(block);
            var order = (TupleTerm)definition.Items[NativeRecordHeapLayout.DefinitionOrder];
            var values = new Term[block.Count];
            for (int index = 0; index < values.Length; index++)
                values[index] = block[(int)((Integer)order.Items[index]).Value];

            return Array.AsReadOnly(values);
        }
    }

    private void ForgetRecordDefinitions(long[] dead)
    {
        foreach (long id in dead) recordDefinitions.Remove(id);
    }
}
