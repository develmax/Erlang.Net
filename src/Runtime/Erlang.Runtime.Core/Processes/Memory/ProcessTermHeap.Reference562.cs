namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal bool IsOrdinaryLocalReference562(Term value)
        => value.GetType() == typeof(ReferenceTerm)
            && value is ReferenceTerm { IsOrdinaryLocal562: true } reference
            && ReferenceEquals(reference.LocalIdentity, runtime.LocalIdentity);

    internal ReferenceTerm CreateReference562(ReferenceTerm source, ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (!IsOrdinaryLocalReference562(source)
            || source.OwnedStorage562 is { } storage && !ReferenceEquals(storage.Owner, this))
            throw new InvalidOperationException(OrdinaryReference562Names.Provenance);
        RequireReferencePlatform562();
        IReadOnlyList<uint> words = source.Words;
        if (words.Count != OrdinaryReference562Names.NumberCount)
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        ProcessTermSequence506 payload = AllocateEncoded526(
            [],
            [OrdinaryReference562Names.Header],
            [((nuint)words[0] << OrdinaryReference562Names.NumberBits) | OrdinaryReference562Names.Marker,
                words[1] | ((nuint)words[2] << OrdinaryReference562Names.NumberBits)]
        );
        try
        {
            var result = new ReferenceTerm(source.LocalIdentity!, payload);
            construction.Publish(result);

            return result;
        }
        catch
        {
            ReleaseRegisteredHeader526(payload);
            throw;
        }
    }

    internal IReadOnlyList<uint> ReadReferenceWords562(ProcessTermSequence506 payload)
    {
        lock (Gate)
        {
            RequireReferencePlatform562();
            if (!ReferenceEquals(payload.Owner, this)
                || !blocks.TryGetValue(payload.Identity, out var active)
                || !ReferenceEquals(active, payload))
                throw new ObjectDisposedException(nameof(ProcessTermSequence506));
            if (payload.Count != 0 || payload.HeaderWords526 != OrdinaryReference562Names.HeaderWords
                || payload.RawPayloadWords556 != OrdinaryReference562Names.DataWords
                || payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526) != OrdinaryReference562Names.Header)
                throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
            int start = checked(payload.AllocationOffset526 + OrdinaryReference562Names.HeaderWords);
            nuint first = payload.Slab.Cells.ReadNumeric556(start);
            nuint second = payload.Slab.Cells.ReadNumeric556(checked(start + 1));
            if ((uint)first != OrdinaryReference562Names.Marker)
                throw new InvalidOperationException(CellStorage526Names.InvalidHeader);

            return Array.AsReadOnly(new uint[]
            {
                (uint)(first >> OrdinaryReference562Names.NumberBits),
                (uint)second,
                (uint)(second >> OrdinaryReference562Names.NumberBits)
            });
        }
    }

    private static void RequireReferencePlatform562()
    {
        if (IntPtr.Size != sizeof(ulong) || !BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException(OrdinaryReference562Names.Platform);
    }
}
