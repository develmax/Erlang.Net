// Port of OTP-29.1.1 dets_v9.erl storage transitions, Apache-2.0.
// Copyright Ericsson AB 2001-2026. See retained upstream source notices.
using System.Buffers.Binary;

namespace Erlang;

/// <summary>Incremental, server-serialized v9 storage; objects remain on disk.</summary>
internal sealed class DetsV9File
{
    private readonly Stream stream;
    private readonly EtfDecodeOptions decode;
    private readonly ProcessRuntime? runtime;
    private DetsBuddyAllocator allocator;
    private readonly List<(long Address, int Exponent)> deferred = new();
    private readonly uint[] counters;
    private bool saved = true;
    private long freelist;

    internal DetsV9File(
        Stream stream,
        bool readOnly,
        EtfDecodeOptions? decode = null,
        ProcessRuntime? runtime = null
    )
    {
        this.stream = stream;
        this.runtime = runtime;
        this.decode = decode ?? new EtfDecodeOptions();
        var reader = new DetsV9Reader(stream, this.decode, runtime: runtime);
        var header = reader.Header;
        allocator = DetsBuddyAllocator.Restore(header.Base, reader.ReadFreeBlocks());
        Type = header.Type;
        KeyPosition = (int)header.KeyPosition;
        M = header.M;
        N = header.N;
        Next = header.Next;
        MinimumSlots = header.MinimumSlots;
        MaximumSlots = header.MaximumSlots;
        HashMethod = header.HashMethod;
        ObjectCount = header.ObjectCount;
        KeyCount = header.KeyCount;
        HasDigest = header.HasDigest;
        counters = header.CollectionCounters.ToArray();
        HasCollectionCounts = counters.Any(value => value != 0) || ObjectCount == 0;
        freelist = header.Freelist;
        ReadOnly = readOnly;
    }

    internal DetsFileType Type { get; }
    internal int KeyPosition { get; }
    internal uint M { get; private set; }
    internal uint N { get; private set; }
    internal uint Next { get; private set; }
    internal uint MinimumSlots { get; }
    internal uint MaximumSlots { get; }
    internal uint HashMethod { get; }
    internal long ObjectCount { get; private set; }
    internal long KeyCount { get; private set; }
    internal bool HasDigest { get; }
    internal bool HasCollectionCounts { get; }
    internal bool ReadOnly { get; }
    internal bool Fixed { get; private set; }
    internal long Length => stream.Length;
    internal bool Dirty => !saved;
    internal bool NeedsGrowth => !ReadOnly && !Fixed && Next < MaximumSlots && KeyCount > Next;

    internal void MayGrow()
    {
        if (!NeedsGrowth)
        {
            return;
        }
        MarkDirty();
        Grow(0);
    }

    internal IReadOnlyList<(uint Slot, long Address, int Exponent, uint Size)> Blocks()
    {
        var blocks = new List<(uint Slot, long Address, int Exponent, uint Size)>();
        for (uint slot = 0; slot < Next; slot++)
        {
            long position = SlotPosition(slot);
            uint size = Word(position);
            uint address = Word(position + DetsV9Layout.WordBytes);
            if (address != 0)
            {
                blocks.Add((slot, address, DetsBuddyAllocator.Exponent(size), size));
            }
        }

        return blocks.OrderBy(block => block.Address).ToArray();
    }

    internal byte[] ReadBlock(long address, int count, int minimum)
    {
        if (address < allocator.Base || address > stream.Length || count < minimum)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        var bytes = new byte[count];
        stream.Position = address;
        _ = stream.ReadAtLeast(bytes, minimum, throwOnEndOfStream: true);

        return bytes;
    }

    internal void Touch() => MarkDirty();

    internal byte[] ReadCollection(long address)
    {
        uint size = Word(address);
        if (size < DetsV9Layout.ObjectHeaderBytes || size > int.MaxValue
            || address < allocator.Base || address + size > stream.Length
            || Word(address + DetsV9Layout.WordBytes) != DetsV9Layout.Active)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }

        return ReadBlock(address, (int)size, (int)size);
    }

    internal uint Slot(Term key)
    {
        uint hash = HashMethod == 1 ? ErlangPortableHash.Phash2(key)
            : ErlangPortableHash.Phash(key, DetsV9Layout.LegacyHashRange) - 1;
        uint slot = hash % M;

        return slot < N ? hash % (M * 2) : slot;
    }

    internal Term Key(Term value)
    {
        if (value is not TupleTerm tuple || tuple.Items.Count < KeyPosition)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return tuple.Items[KeyPosition - 1];
    }

    internal IReadOnlyList<Term> ReadSlot(uint slot)
        => new DetsV9Reader(
            stream,
            decode,
            allowDirty: true,
            runtime: runtime,
            liveSlotCount: Next
        ).ReadSlot(slot);

    internal IReadOnlyList<Term> Lookup(Term key)
        => ReadSlot(Slot(key)).Where(value => Key(value).Equals(key)).ToArray();

    internal IEnumerable<Term> Objects()
    {
        for (uint slot = 0; slot < Next; slot++)
        {
            foreach (Term item in ReadSlot(slot))
            {
                yield return item;
            }
        }
    }

    internal bool Insert(IReadOnlyList<Term> values, bool onlyNew)
    {
        RequireWritable();
        Term[] keys = values.Select(Key).ToArray();
        // Encode and validate the entire request before admitting any mutation.
        foreach (Term value in values)
        {
            _ = ExternalTermFormat.EncodedSize(value, new EtfEncodingOptions());
        }
        if (onlyNew && keys.Distinct().Any(key => Lookup(key).Count != 0))
        {
            return false;
        }
        if (values.Count == 0)
        {
            return true;
        }
        MarkDirty();
        Grow(Math.Min(keys.Distinct().Count(), values.Count));
        foreach (var batch in values.GroupBy(value => Slot(Key(value))))
        {
            var store = new DetsObjectStore(Type, KeyPosition);
            IReadOnlyList<Term> old = ReadSlot(batch.Key);
            store.Insert(old, onlyNew: false);
            store.Insert(batch.ToArray(), onlyNew: false);
            ReplaceSlot(batch.Key, old, store.Snapshot());
        }

        return true;
    }

    internal void Delete(Term key, Term? exactObject = null)
    {
        RequireWritable();
        uint slot = Slot(key);
        IReadOnlyList<Term> old = ReadSlot(slot);
        Term[] remaining = old.Where(value => exactObject is null ? !Key(value).Equals(key) : !value.Equals(exactObject)).ToArray();
        if (remaining.Length == old.Count)
        {
            return;
        }
        MarkDirty();
        ReplaceSlot(slot, old, remaining);
    }

    internal Term UpdateCounter(Term key, Term increment)
    {
        RequireWritable();
        var store = new DetsObjectStore(Type, KeyPosition);
        store.Insert(Lookup(key), onlyNew: false);
        Term result = store.UpdateCounter(key, increment);
        Insert(store.Snapshot(), onlyNew: false);

        return result;
    }

    internal void Clear()
    {
        RequireWritable();
        MarkDirty();
        for (uint slot = 0; slot < Next; slot++)
        {
            IReadOnlyList<Term> old = ReadSlot(slot);
            if (old.Count != 0)
            {
                ReplaceSlot(slot, old, Array.Empty<Term>());
            }
        }
    }

    internal void SetFixed(bool value)
    {
        if (Fixed == value)
        {
            return;
        }
        Fixed = value;
        if (!value)
        {
            foreach (var block in deferred)
            {
                allocator.Release(block.Address, block.Exponent);
            }
            deferred.Clear();
        }
    }

    internal void Save(bool durable)
    {
        if (ReadOnly || saved)
        {
            if (durable && stream is FileStream unchanged)
            {
                unchanged.Flush(flushToDisk: true);
            }

            return;
        }
        freelist = stream.Length;
        stream.Position = freelist;
        var actual = allocator.Clone();
        foreach (var block in deferred)
        {
            actual.Release(block.Address, block.Exponent);
        }
        DetsV9Writer.WriteFreeLists(stream, actual);
        long finalLength = stream.Position + DetsV9Layout.WordBytes;
        DetsV9Writer.WriteWord(stream, checked((uint)(finalLength - (allocator.Base == DetsV9Layout.LegacyBase ? 0 : allocator.Base))));
        stream.Flush();
        byte[] header = DetsV9Writer.Header(
            Type,
            KeyPosition,
            checked((int)Next),
            checked((int)MinimumSlots),
            checked((int)MaximumSlots),
            ObjectCount,
            checked((int)KeyCount),
            freelist,
            allocator.Base,
            counters,
            closed: true,
            n: N,
            m: M,
            hashMethod: HashMethod,
            hasDigest: HasDigest
        );
        stream.Position = 0;
        stream.Write(header);
        stream.Flush();
        if (durable && stream is FileStream file)
        {
            file.Flush(flushToDisk: true);
        }
        saved = true;
    }

    internal Term Parameters(string filename)
    {
        if (!HasCollectionCounts)
        {
            return Term.A(DetsNames.Undefined);
        }
        Term counts = Cons.From(counters.Select((count, index) => (count, index))
            .Where(pair => pair.count != 0)
            .Select(pair => (Term)Term.Tuple(Term.I(pair.index + DetsV9Layout.MinimumCollectionExponent), Term.I(pair.count))));

        return Term.Tuple(
            Term.A(DetsNames.HashParameters),
            Term.I(DetsV9Layout.Version),
            Term.I(DetsNames.BchunkVersion),
            Term.String(Path.GetFileName(filename)),
            TypeTerm,
            Term.I(KeyPosition),
            Term.I(HashMethod),
            Term.I(N),
            Term.I(M),
            Term.I(Next),
            Term.I(MinimumSlots),
            Term.I(MaximumSlots),
            Term.I(ObjectCount),
            Term.I(KeyCount),
            counts
        );
    }

    internal Term TypeTerm => Term.A(Type switch
    {
        DetsFileType.Set => DetsNames.Set,
        DetsFileType.Bag => DetsNames.Bag,
        _ => DetsNames.DuplicateBag
    });

    internal IReadOnlyList<(uint Slot, long Address, int Exponent, byte[] Data)> Chunks()
    {
        var result = new List<(uint Slot, long Address, int Exponent, byte[] Data)>();
        for (uint slot = 0; slot < Next; slot++)
        {
            long position = SlotPosition(slot);
            uint size = Word(position);
            uint address = Word(position + DetsV9Layout.WordBytes);
            if (address == 0)
            {
                continue;
            }
            int exponent = DetsBuddyAllocator.Exponent(size);
            var data = new byte[checked((int)(1L << exponent))];
            stream.Position = address;
            int count = stream.ReadAtLeast(data, checked((int)size), throwOnEndOfStream: true);
            data.AsSpan(count).Clear();
            result.Add((slot, address, exponent, data));
        }

        return result.OrderBy(block => block.Address).ToArray();
    }

    internal void ImportChunk(uint slot, byte[] data)
    {
        RequireWritable();
        if (slot >= Next || data.Length < DetsV9Layout.ObjectHeaderBytes)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        uint size = BinaryPrimitives.ReadUInt32BigEndian(data);
        if (size > data.Length || size < DetsV9Layout.ObjectHeaderBytes)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        uint status = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(DetsV9Layout.WordBytes));
        if (status != DetsV9Layout.Active && status != DetsV9Layout.Free)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        var values = DetsV9Recovery.DecodeCollection(
            data,
            Type,
            KeyPosition,
            decode,
            runtime
        );
        if (values.Count == 0 || values.Any(value => Slot(Key(value)) != slot) || Word(SlotPosition(slot)) != 0)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        int exponent = DetsBuddyAllocator.Exponent(size);
        if (data.Length != 1L << exponent)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        MarkDirty();
        var block = allocator.Allocate(data.Length);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(DetsV9Layout.WordBytes), DetsV9Layout.Active);
        stream.Position = block.Address;
        stream.Write(data);
        long pointer = SlotPosition(slot);
        DetsV9Writer.PutWord(stream, pointer, size);
        DetsV9Writer.PutWord(stream, pointer + DetsV9Layout.WordBytes, block.Address);
        counters[exponent - DetsV9Layout.MinimumCollectionExponent]++;
        ObjectCount += values.Count;
        KeyCount += values.Select(Key).Distinct().Count();
    }

    private void MarkDirty()
    {
        RequireWritable();
        if (!saved)
        {
            return;
        }
        DetsV9Writer.PutWord(stream, DetsV9Layout.ClosedOffset, 0);
        stream.Flush();
        if (stream is FileStream file)
        {
            file.Flush(flushToDisk: true);
        }
        stream.SetLength(freelist);
        saved = false;
    }

    private void ReplaceSlot(uint slot, IReadOnlyList<Term> old, IReadOnlyList<Term> values)
    {
        long slotPosition = SlotPosition(slot);
        uint oldSize = Word(slotPosition);
        uint oldAddress = Word(slotPosition + DetsV9Layout.WordBytes);
        var groups = values.GroupBy(Key).OrderBy(group => group.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
            .Select(group => new KeyValuePair<Term, List<Term>>(group.Key, group.ToList())).ToList();
        byte[]? encoded = groups.Count == 0 ? null : DetsV9Writer.EncodeCollection(groups, Type);
        int oldExponent = oldAddress == 0 ? 0 : DetsBuddyAllocator.Exponent(oldSize);
        int newExponent = encoded is null ? 0 : DetsBuddyAllocator.Exponent(encoded.Length);
        long address = oldAddress;
        if (encoded is null || oldAddress == 0 || oldExponent != newExponent)
        {
            address = encoded is null ? 0 : allocator.Allocate(encoded.Length).Address;
            if (oldAddress != 0)
            {
                DetsV9Writer.PutWord(stream, oldAddress + DetsV9Layout.WordBytes, DetsV9Layout.Free);
                if (Fixed)
                {
                    deferred.Add((oldAddress, oldExponent));
                }
                else
                {
                    allocator.Release(oldAddress, oldExponent);
                }
                if (HasCollectionCounts)
                {
                    counters[oldExponent - DetsV9Layout.MinimumCollectionExponent]--;
                }
            }
            if (encoded is not null && HasCollectionCounts)
            {
                counters[newExponent - DetsV9Layout.MinimumCollectionExponent]++;
            }
        }
        if (encoded is not null)
        {
            stream.Position = address;
            stream.Write(encoded);
        }
        DetsV9Writer.PutWord(stream, slotPosition, encoded?.Length ?? 0);
        DetsV9Writer.PutWord(stream, slotPosition + DetsV9Layout.WordBytes, address);
        ObjectCount += values.Count - old.Count;
        KeyCount += groups.Count - old.Select(Key).Distinct().Count();
    }

    private void Grow(int maximumNewKeys)
    {
        if (Fixed || Next >= MaximumSlots)
        {
            return;
        }
        long extra = Math.Min(DetsV9Layout.SlotsPerSegment * 2, KeyCount + maximumNewKeys - Next);
        while (extra > 0 && Next < MaximumSlots)
        {
            uint segment = Next / DetsV9Layout.SlotsPerSegment;
            long partPosition = DetsV9Layout.HeaderBytes + segment / DetsV9Layout.SegmentsPerPart * DetsV9Layout.WordBytes;
            long part = Word(partPosition);
            if (part == 0)
            {
                part = AllocateDirectory();
                DetsV9Writer.PutWord(stream, partPosition, part);
            }
            long destination = AllocateDirectory();
            DetsV9Writer.PutWord(stream, part + segment % DetsV9Layout.SegmentsPerPart * DetsV9Layout.WordBytes, destination);
            uint start = N;
            Next += DetsV9Layout.SlotsPerSegment;
            // Keep the last saved header (and its digest) intact until Save.
            // Dirty readers receive current addressing from this worker instead.
            for (uint offset = 0; offset < DetsV9Layout.SlotsPerSegment; offset++)
            {
                uint oldSlot = start + offset;
                uint newSlot = oldSlot + M;
                IReadOnlyList<Term> old = ReadSlot(oldSlot);

                uint Hash(Term value) => HashMethod == 1 ? ErlangPortableHash.Phash2(Key(value))
                    : ErlangPortableHash.Phash(Key(value), DetsV9Layout.LegacyHashRange) - 1;
                Term[] moved = old.Where(value => Hash(value) % (M * 2) >= M).ToArray();
                if (moved.Length == 0)
                {
                    continue;
                }
                ReplaceSlot(oldSlot, old, old.Where(value => Hash(value) % (M * 2) < M).ToArray());
                ReplaceSlot(newSlot, Array.Empty<Term>(), moved);
            }
            N += DetsV9Layout.SlotsPerSegment;
            if (N == M)
            {
                M *= 2;
                N = 0;
            }
            extra -= DetsV9Layout.SlotsPerSegment;
        }
    }

    private long AllocateDirectory()
    {
        long address = allocator.Allocate(DetsV9Layout.SegmentBytes).Address;
        stream.Position = address;
        stream.Write(new byte[DetsV9Layout.SegmentBytes]);

        return address;
    }

    private long SlotPosition(uint slot)
        => new DetsV9Reader(stream, decode, allowDirty: true).SlotPosition(slot);

    private uint Word(long position)
    {
        Span<byte> bytes = stackalloc byte[DetsV9Layout.WordBytes];
        stream.Position = position;
        stream.ReadExactly(bytes);

        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    private void RequireWritable()
    {
        if (ReadOnly)
        {
            throw new DetsFileException(DetsNames.AccessMode);
        }
    }
}
