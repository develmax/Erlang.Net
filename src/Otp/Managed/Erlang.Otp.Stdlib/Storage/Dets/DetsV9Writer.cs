// Derived from Erlang/OTP 29.1.1 dets_v9.erl (Apache-2.0).
// Copyright Ericsson AB 2001-2026. Upstream notices are retained in Otp/dets_v9.erl.
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Erlang;

/// <summary>Writes compact, cleanly closed v9(d) images. The hash provider must implement OTP phash2/1.</summary>
/// <remarks>This image builder is not a replacement for the DETS server's dirty/save protocol.</remarks>
internal static class DetsV9Writer
{
    public static void Write(
        Stream destination,
        IEnumerable<Term> objects,
        DetsFileType type,
        int keyPosition,
        Func<Term, uint> hash,
        int minimumSlots = DetsV9Layout.MinimumSlots,
        int maximumSlots = DetsV9Layout.MaximumSlots
    )
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(hash);
        if (!destination.CanWrite || !destination.CanSeek)
        {
            throw new ArgumentException(DetsFileReasons.InvalidOutput, nameof(destination));
        }
        if (type is not DetsFileType.Set and not DetsFileType.Bag and not DetsFileType.DuplicateBag || keyPosition <= 0
            || minimumSlots < DetsV9Layout.MinimumSlots || maximumSlots < minimumSlots
            || maximumSlots > DetsV9Layout.MaximumSlots)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        minimumSlots = RoundSlots(minimumSlots);
        maximumSlots = RoundSlots(maximumSlots);
        var keyed = new Dictionary<Term, List<Term>>();
        foreach (Term value in objects)
        {
            if (value is not TupleTerm tuple || tuple.Items.Count < keyPosition)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            Term key = tuple.Items[keyPosition - 1];
            if (!keyed.TryGetValue(key, out List<Term>? values))
            {
                values = new List<Term>();
                keyed.Add(key, values);
            }
            if (type == DetsFileType.Set)
            {
                values.Clear();
            }
            if (type != DetsFileType.Bag || !values.Contains(value))
            {
                values.Add(value);
            }
        }
        int slots = Math.Min(maximumSlots, Math.Max(minimumSlots, RoundSlots(Math.Max(1, keyed.Count))));
        var buckets = new SortedDictionary<int, List<KeyValuePair<Term, List<Term>>>>();
        foreach (var pair in keyed)
        {
            int slot = (int)(hash(pair.Key) & (uint)(slots - 1));
            if (!buckets.TryGetValue(slot, out var keys))
            {
                keys = new List<KeyValuePair<Term, List<Term>>>();
                buckets.Add(slot, keys);
            }
            keys.Add(pair);
        }
        // Allocate permanent directory blocks first, exactly as init_file/17 does.
        int segmentCount = slots / DetsV9Layout.SlotsPerSegment;
        int partCount = (segmentCount + DetsV9Layout.SegmentsPerPart - 1) / DetsV9Layout.SegmentsPerPart;
        long allocationBase = DetsV9Layout.LegacyBase
            + (long)(partCount + segmentCount) * DetsV9Layout.SegmentBytes;
        var allocator = new DetsBuddyAllocator(allocationBase);
        var counters = new uint[DetsV9Layout.MaximumCollectionExponent - DetsV9Layout.MinimumCollectionExponent + 1];
        long objectCount = keyed.Values.Sum(values => (long)values.Count);
        // Every fallible object encode happens before mutating the destination.
        var encodedBuckets = new List<(int Slot, long Address, int Exponent, byte[] Bytes)>();
        long endOfObjects = allocationBase;
        foreach (var bucket in buckets)
        {
            bucket.Value.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
            byte[] encoded = EncodeCollection(bucket.Value, type);
            var block = allocator.Allocate(encoded.Length);
            if (block.Exponent < DetsV9Layout.MinimumCollectionExponent)
            {
                throw new DetsFileException(DetsFileReasons.BadObject);
            }
            counters[block.Exponent - DetsV9Layout.MinimumCollectionExponent]++;
            encodedBuckets.Add((bucket.Key, block.Address, block.Exponent, encoded));
            endOfObjects = Math.Max(endOfObjects, block.Address + (1L << block.Exponent));
        }
        destination.SetLength(0);
        destination.SetLength(endOfObjects);
        byte[] dirtyHeader = Header(
            type,
            keyPosition,
            slots,
            minimumSlots,
            maximumSlots,
            objectCount,
            keyed.Count,
            0,
            allocationBase,
            counters,
            closed: false
        );
        destination.Position = 0;
        destination.Write(dirtyHeader);
        for (int part = 0; part < partCount; part++)
        {
            long partAddress = DetsV9Layout.LegacyBase + (long)part * DetsV9Layout.PartBytes;
            PutWord(destination, DetsV9Layout.HeaderBytes + part * DetsV9Layout.WordBytes, partAddress);
            for (int index = 0; index < DetsV9Layout.SegmentsPerPart; index++)
            {
                int segment = part * DetsV9Layout.SegmentsPerPart + index;
                if (segment >= segmentCount)
                {
                    break;
                }
                long segmentAddress = DetsV9Layout.LegacyBase
                    + (long)partCount * DetsV9Layout.PartBytes + (long)segment * DetsV9Layout.SegmentBytes;
                PutWord(destination, partAddress + index * DetsV9Layout.WordBytes, segmentAddress);
            }
        }
        foreach (var bucket in encodedBuckets)
        {
            long segmentAddress = DetsV9Layout.LegacyBase + (long)partCount * DetsV9Layout.PartBytes
                + (long)(bucket.Slot / DetsV9Layout.SlotsPerSegment) * DetsV9Layout.SegmentBytes;
            long slotAddress = segmentAddress + bucket.Slot % DetsV9Layout.SlotsPerSegment * DetsV9Layout.SlotBytes;
            PutWord(destination, slotAddress, bucket.Bytes.Length);
            PutWord(destination, slotAddress + DetsV9Layout.WordBytes, bucket.Address);
            destination.Position = bucket.Address;
            destination.Write(bucket.Bytes);
        }
        destination.Position = endOfObjects;
        WriteFreeLists(destination, allocator);
        long fileLength = destination.Position + DetsV9Layout.WordBytes;
        WriteWord(destination, checked((uint)(fileLength - allocationBase)));
        destination.Flush();
        byte[] cleanHeader = Header(
            type,
            keyPosition,
            slots,
            minimumSlots,
            maximumSlots,
            objectCount,
            keyed.Count,
            endOfObjects,
            allocationBase,
            counters,
            closed: true
        );
        destination.Position = 0;
        destination.Write(cleanHeader);
        destination.Flush();
    }

    internal static byte[] EncodeCollection(
        List<KeyValuePair<Term, List<Term>>> keys,
        DetsFileType type
    )
    {
        using var collection = new MemoryStream();
        WriteWord(collection, 0);
        WriteWord(collection, DetsV9Layout.Active);
        foreach (var pair in keys)
        {
            long groupStart = collection.Position;
            if (type != DetsFileType.Set)
            {
                WriteWord(collection, 0);
            }
            foreach (Term term in pair.Value)
            {
                byte[] encoded = ExternalTermFormat.Encode(term);
                WriteWord(collection, checked((uint)(encoded.Length + DetsV9Layout.WordBytes)));
                collection.Write(encoded);
            }
            if (type != DetsFileType.Set)
            {
                long end = collection.Position;
                PutWord(collection, groupStart, end - groupStart);
                collection.Position = end;
            }
        }
        PutWord(collection, 0, collection.Length);

        return collection.ToArray();
    }

    internal static byte[] Header(
        DetsFileType type,
        int keyPosition,
        int slots,
        int minimumSlots,
        int maximumSlots,
        long objectCount,
        int keyCount,
        long freelist,
        long allocationBase,
        uint[] counters,
        bool closed,
        uint n = 0,
        uint? m = null,
        uint hashMethod = 1,
        bool hasDigest = true
    )
    {
        var bytes = new byte[DetsV9Layout.HeaderBytes];
        uint[] words =
        [
            checked((uint)freelist), DetsV9Layout.Cookie, closed ? 1U : 0U,
            (uint)type, DetsV9Layout.Version, m ?? (uint)slots, (uint)slots,
            (uint)keyPosition, checked((uint)objectCount), (uint)keyCount,
            (uint)minimumSlots, (uint)maximumSlots, hashMethod, n
        ];
        for (int index = 0; index < words.Length; index++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(index * DetsV9Layout.WordBytes), words[index]);
        }
        for (int index = 0; index < counters.Length; index++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(DetsV9Layout.CountersOffset + index * DetsV9Layout.WordBytes),
                counters[index]
            );
        }
        if (hasDigest)
        {
            MD5.HashData(bytes.AsSpan(DetsV9Layout.DigestStart, DetsV9Layout.DigestOffset - DetsV9Layout.DigestStart))
                .CopyTo(bytes, DetsV9Layout.DigestOffset);
        }
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(DetsV9Layout.BaseOffset),
            allocationBase == DetsV9Layout.LegacyBase ? 0 : checked((uint)allocationBase));

        return bytes;
    }

    internal static void WriteFreeLists(Stream destination, DetsBuddyAllocator allocator)
    {
        int maximumAddresses = (DetsV9Layout.MaximumFreeRecordBytes - DetsV9Layout.FreeHeaderBytes) / DetsV9Layout.WordBytes;
        foreach (var level in allocator.FreeBlocks.GroupBy(block => block.Exponent))
        {
            foreach (var chunk in level.Chunk(maximumAddresses))
            {
                WriteWord(destination, checked((uint)(DetsV9Layout.FreeHeaderBytes + chunk.Length * DetsV9Layout.WordBytes)));
                WriteWord(destination, DetsV9Layout.Free);
                WriteWord(destination, checked((uint)(level.Key + 1)));
                foreach (var block in chunk)
                {
                    WriteWord(destination, checked((uint)block.Address));
                }
            }
        }
        WriteWord(destination, DetsV9Layout.FreeHeaderBytes);
        WriteWord(destination, DetsV9Layout.Free);
        WriteWord(destination, DetsV9Layout.EndFree);
    }

    private static int RoundSlots(int value)
    {
        int slots = DetsV9Layout.MinimumSlots;
        while (slots < value && slots < DetsV9Layout.MaximumSlots)
        {
            slots <<= 1;
        }

        return slots;
    }

    internal static void PutWord(Stream destination, long position, long value)
    {
        destination.Position = position;
        WriteWord(destination, checked((uint)value));
    }

    internal static void WriteWord(Stream destination, uint value)
    {
        Span<byte> word = stackalloc byte[DetsV9Layout.WordBytes];
        BinaryPrimitives.WriteUInt32BigEndian(word, value);
        destination.Write(word);
    }
}
