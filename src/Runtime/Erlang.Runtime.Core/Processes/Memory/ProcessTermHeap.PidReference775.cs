namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    // Provenance is established by the internal runtime constructor, never by
    // matching public node/creation/numeric values or reading a foreign heap.
    internal bool IsLocalPidReference775(Term value)
        => value.GetType() == typeof(ReferenceTerm)
            && value is ReferenceTerm { IsLocalPid775: true } reference
            && ReferenceEquals(reference.LocalIdentity, runtime.LocalIdentity);

    internal ReferenceTerm CreatePidReference775(ReferenceTerm source, ProcessTermConstruction514 construction)
    {
        if (!Monitor.IsEntered(Gate))
            throw new InvalidOperationException(CellStorage526Names.GateRequired);
        if (!IsLocalPidReference775(source)
            || source.OwnedStorage562 is { } storage && !ReferenceEquals(storage.Owner, this))
            throw new InvalidOperationException(PidReference775Names.Provenance);
        RequireReferencePlatform562();
        IReadOnlyList<uint> words = source.Words;
        if (!ReferenceTerm.HasLocalPidNumbers775(words))
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        ProcessTermSequence506 payload = AllocateEncoded526(
            [],
            [PidReference775Names.Header],
            [((nuint)words[0] << ReferenceIdentityLayout.WordBits) | OrdinaryReference562Names.Marker,
                words[1] | ((nuint)words[2] << ReferenceIdentityLayout.WordBits),
                words[ReferenceIdentityLayout.PidOwnerLowWord] | ((nuint)words[ReferenceIdentityLayout.PidOwnerHighWord] << ReferenceIdentityLayout.WordBits)]
        );
        try
        {
            var result = new ReferenceTerm(source, payload);
            construction.Publish(result);

            return result;
        }
        catch
        {
            ReleaseRegisteredHeader526(payload);
            throw;
        }
    }

    // Caller already validated this registered live block under H. Header and
    // all five numbers are read from native cells on every public numeric read.
    private bool TryReadPidReferenceWords775(ProcessTermSequence506 payload, out IReadOnlyList<uint>? numbers)
    {
        numbers = null;
        if (payload.RawPayloadWords556 != PidReference775Names.DataWords)
            return false;
        if (payload.Count != 0 || payload.HeaderWords526 != PidReference775Names.HeaderWords
            || payload.Slab.Cells.ReadHeader526(payload.AllocationOffset526) != PidReference775Names.Header)
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        int start = checked(payload.AllocationOffset526 + PidReference775Names.HeaderWords);
        nuint first = payload.Slab.Cells.ReadNumeric556(start);
        nuint second = payload.Slab.Cells.ReadNumeric556(checked(start + 1));
        nuint third = payload.Slab.Cells.ReadNumeric556(checked(start + 2));
        if ((uint)first != OrdinaryReference562Names.Marker)
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        uint[] words = [(uint)(first >> ReferenceIdentityLayout.WordBits),
            (uint)second, (uint)(second >> ReferenceIdentityLayout.WordBits),
            (uint)third, (uint)(third >> ReferenceIdentityLayout.WordBits)];
        if (!ReferenceTerm.HasLocalPidNumbers775(words))
            throw new InvalidOperationException(CellStorage526Names.InvalidHeader);
        numbers = Array.AsReadOnly(words);

        return true;
    }
}
