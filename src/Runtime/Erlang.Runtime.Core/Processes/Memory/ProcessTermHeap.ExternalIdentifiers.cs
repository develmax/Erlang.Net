namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal bool CanOwnExternalIdentifier(Term source)
        => source switch
        {
            Pid pid => pid.LocalIdentity is null && !runtime.IsSharingLiteral425(source),
            PortTerm port => port.LocalIdentity is null && !runtime.IsSharingLiteral425(source),
            ReferenceTerm reference => source.GetType() == typeof(ReferenceTerm) && reference.LocalIdentity is null && !runtime.IsSharingLiteral425(source),
            _ => false
        };

    internal Term CreateExternalIdentifier(Term source, ProcessTermConstruction514 construction, PreparedExternalIdentifier prepared)
    {
        if (!Monitor.IsEntered(Gate) || closing || !frames.Contains(construction.Frame517)
            || construction.Frame517.IsDisposed || !construction.Contains(source) || !CanOwnExternalIdentifier(source))
            throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);
        RequireReferencePlatform562();
        construction.PrepareMagicTracking576();
        magicReferences576.EnsureCapacity(checked(magicReferences576.Count + 1));
        magicAddresses576.EnsureCapacity(checked(magicAddresses576.Count + 1));
        PhysicalNodeLease? lease = prepared.Node.Retain();
        NativeOffheapHeader576? owned = null;
        ProcessTermSequence506? block = null;
        nint previous = magicOffHeap576;
        bool byIdentity = false, byAddress = false, linked = false;
        try
        {
            nuint[] data = new nuint[checked(2 + prepared.Data.Length)];
            data[0] = (nuint)lease.Address;
            data[1] = (nuint)previous;
            prepared.Data.CopyTo(data, 2);
            block = AllocateEncoded526([], [ExternalIdentifierLayout.Header(prepared.Kind, prepared.Data.Length)], data);
            nint address = CaptureRawAddress571(block, 0, block.AllocationWords526).Address571;
            owned = new(
                this,
                block,
                prepared.Kind,
                address,
                lease
            );
            magicReferences576.Add(block.Identity, owned);
            byIdentity = true;
            magicAddresses576.Add(address, owned);
            byAddress = true;
            construction.TrackMagic576(owned);
            magicOffHeap576 = address;
            linked = true;
            lease = null;
            Term result = prepared.Kind switch
            {
                NativeOffheapKind576.ExternalPid => new Pid(block),
                NativeOffheapKind576.ExternalPort => new PortTerm(block),
                NativeOffheapKind576.ExternalReference => new ReferenceTerm((IOwnedExternalIdentifierStorage)block),
                _ => throw new InvalidOperationException(ExternalIdentifierLayout.Invalid)
            };
            construction.Publish(result);

            return result;
        }
        catch
        {
            if (linked) magicOffHeap576 = previous;
            if (byAddress) magicAddresses576.Remove(owned!.Address);
            if (byIdentity) magicReferences576.Remove(block!.Identity);
            if (block is not null) ReleaseRegisteredHeader526(block);
            if (owned is not null) owned.Release576(false);
            else lease?.Dispose();
            throw;
        }
    }

    private NativeOffheapHeader576 RequireExternalIdentifier(ProcessTermSequence506 block)
    {
        if (!ReferenceEquals(block.Owner, this) || !blocks.TryGetValue(block.Identity, out var live) || !ReferenceEquals(live, block))
            throw new ObjectDisposedException(nameof(ProcessTermSequence506));
        if (!magicReferences576.TryGetValue(block.Identity, out var entry) || !ExternalIdentifierLayout.IsKind(entry.Kind))
            throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);

        return RequireNativeOffheapHeader576(block, entry.Kind);
    }

    internal LocalNodeSnapshot ReadExternalIdentity(ProcessTermSequence506 block)
    {
        lock (Gate) return RequireExternalIdentifier(block).ExternalNode!.Snapshot;
    }

    internal ulong ReadExternalId(ProcessTermSequence506 block)
    {
        lock (Gate)
        {
            var entry = RequireExternalIdentifier(block);
            if (entry.Kind == NativeOffheapKind576.ExternalReference || block.AllocationWords526 != ExternalIdentifierLayout.HeadWords + 1)
                throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);

            return (ulong)block.Slab.Cells.ReadNumeric556(block.AllocationOffset526 + ExternalIdentifierLayout.DataWord);
        }
    }

    internal IReadOnlyList<uint> ReadExternalReferenceWords(ProcessTermSequence506 block)
    {
        lock (Gate)
        {
            var entry = RequireExternalIdentifier(block);
            if (entry.Kind != NativeOffheapKind576.ExternalReference) throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);
            int start = block.AllocationOffset526 + ExternalIdentifierLayout.DataWord;
            uint count = (uint)block.Slab.Cells.ReadNumeric556(start);
            if (count < ReferenceIdentityLayout.MinimumWords || count > ReferenceIdentityLayout.MaximumWords
                || block.AllocationWords526 != ExternalIdentifierLayout.HeadWords + (int)count / 2 + 1)
                throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);
            var numbers = new uint[count];
            for (int index = 0; index < numbers.Length; index++)
                numbers[index] = (uint)(block.Slab.Cells.ReadNumeric556(start + (index + 1) / 2)
                    >> ((index + 1) % 2 * ExternalIdentifierLayout.UintBits));
            if (count % 2 == 0 && block.Slab.Cells.ReadNumeric556(start + (int)count / 2) >> ExternalIdentifierLayout.UintBits != 0)
                throw new InvalidOperationException(ExternalIdentifierLayout.Invalid);

            return Array.AsReadOnly(numbers);
        }
    }
}
