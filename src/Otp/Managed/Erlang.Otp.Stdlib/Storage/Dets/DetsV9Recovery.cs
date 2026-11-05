// Derived from OTP-29.1.1 dets_v9 fsck_input and dets fsck, Apache-2.0.
// Copyright Ericsson AB 2001-2026. Original sources and notices are retained.
using System.Buffers.Binary;

namespace Erlang;

internal static class DetsV9Recovery
{
    internal static void Compact(
        Stream source,
        Stream output,
        DetsV9Header header,
        ProcessRuntime runtime
    )
    {
        DetsV9Writer.Write(
            output,
            Array.Empty<Term>(),
            header.Type,
            (int)header.KeyPosition,
            ErlangPortableHash.Phash2,
            (int)header.Next,
            (int)header.MaximumSlots
        );
        var empty = DetsV9Header.Read(output);
        byte[] metadata = DetsV9Writer.Header(
            header.Type,
            (int)header.KeyPosition,
            (int)header.Next,
            (int)header.MinimumSlots,
            (int)header.MaximumSlots,
            0,
            0,
            empty.Freelist,
            empty.Base,
            new uint[header.CollectionCounters.Count],
            true,
            header.N,
            header.M,
            header.HashMethod,
            header.HasDigest
        );
        output.Position = 0;
        output.Write(metadata);
        var destination = new DetsV9File(output, false, runtime: runtime);
        var original = new DetsV9File(source, true, runtime: runtime);
        foreach (var block in original.Blocks())
        {
            byte[] data = original.ReadBlock(block.Address, checked((int)(1L << block.Exponent)), checked((int)block.Size));
            destination.ImportChunk(block.Slot, data);
        }
        destination.Save(durable: true);
    }

    internal static IReadOnlyList<Term> DecodeCollection(
        byte[] bytes,
        DetsFileType type,
        int keyPosition,
        EtfDecodeOptions? options = null,
        ProcessRuntime? runtime = null
    )
    {
        if (bytes.Length < DetsV9Layout.ObjectHeaderBytes)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        int end = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes));
        if (end > bytes.Length || end < DetsV9Layout.ObjectHeaderBytes)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        var values = new List<Term>();
        int cursor = DetsV9Layout.ObjectHeaderBytes;
        while (cursor < end)
        {
            int groupEnd = end;
            if (type != DetsFileType.Set)
            {
                int groupSize = Length(bytes, cursor, end);
                groupEnd = checked(cursor + groupSize);
                cursor += DetsV9Layout.WordBytes;
            }
            do
            {
                int size = Length(bytes, cursor, groupEnd);
                if (size <= DetsV9Layout.WordBytes)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                ReadOnlySpan<byte> encoded = bytes.AsSpan(cursor + DetsV9Layout.WordBytes, size - DetsV9Layout.WordBytes);
                Term term;
                try
                {
                    term = runtime is null ? ExternalTermFormat.DecodeOne(encoded, options ?? new EtfDecodeOptions(), size).Term
                        : runtime.ImportExternalTerm(encoded, options, size).Term;
                }
                catch (ErlangException)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject, DetsProtocolNames.ScanFunction);
                }
                if (term is not TupleTerm tuple || tuple.Items.Count < keyPosition)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                values.Add(term);
                cursor += size;
                if (type == DetsFileType.Set)
                {
                    break;
                }
            }
            while (cursor < groupEnd);
        }

        return values;
    }

    internal static IEnumerable<Term> Scan(
        Stream source,
        DetsV9Header header,
        EtfDecodeOptions? options = null,
        ProcessRuntime? runtime = null
    )
    {
        long maximum = Math.Max(
            DetsNames.ChunkBytes,
            header.HasDigest && header.CollectionCounters.Any(value => value != 0)
                ? 1L << (header.CollectionCounters.Select((count, index) => (count, index))
                    .Where(pair => pair.count != 0).Max(pair => pair.index) + DetsV9Layout.MinimumCollectionExponent)
                : source.Length
        );
        long position = DetsV9Layout.LegacyBase;
        var prefix = new byte[DetsV9Layout.ObjectHeaderBytes];
        while (position <= source.Length - prefix.Length)
        {
            source.Position = position;
            source.ReadExactly(prefix);
            uint size = BinaryPrimitives.ReadUInt32BigEndian(prefix);
            uint status = BinaryPrimitives.ReadUInt32BigEndian(prefix.AsSpan(DetsV9Layout.WordBytes));
            IReadOnlyList<Term>? recovered = null;
            if (status == DetsV9Layout.Active && size >= DetsV9Layout.ObjectHeaderBytes
                && size <= maximum && size <= int.MaxValue && position + size <= source.Length)
            {
                var bytes = new byte[(int)size];
                source.Position = position;
                source.ReadExactly(bytes);
                try
                {
                    recovered = DecodeCollection(
                        bytes,
                        header.Type,
                        checked((int)header.KeyPosition),
                        options,
                        runtime
                    );
                }
                catch (Exception error) when (error is ErlangException or DetsFileException or OverflowException)
                {
                    // OTP skips the entire malformed collection, then resumes at BUMP.
                }
            }
            if (recovered is null)
            {
                position += DetsV9Layout.RepairAlignment;
                continue;
            }
            foreach (Term value in recovered)
            {
                yield return value;
            }
            position += 1L << DetsBuddyAllocator.Exponent(size);
        }
    }

    internal static void Rewrite(
        Stream source,
        Stream output,
        DetsV9Header header,
        DetsOpenOptions options,
        ProcessRuntime? runtime = null
    )
    {
        if (header.Cookie != DetsV9Layout.Cookie || header.TypeCode is < 1 or > 3
            || header.Version != DetsV9Layout.Version || header.KeyPosition == 0 || header.KeyPosition > int.MaxValue)
        {
            throw new DetsFileException(DetsFileReasons.NotDets);
        }
        int min = options.MinimumSlots is { } minimum ? (int)System.Numerics.BigInteger.Min(minimum, DetsV9Layout.MaximumSlots)
            : Math.Max(DetsV9Layout.MinimumSlots, (int)header.MinimumSlots);
        int max = options.MaximumSlots is { } maximum ? (int)System.Numerics.BigInteger.Min(maximum, DetsV9Layout.MaximumSlots)
            : (int)header.MaximumSlots;
        max = Math.Clamp(max, DetsV9Layout.MinimumSlots, DetsV9Layout.MaximumSlots);
        min = Math.Min(min, max);
        DetsV9Writer.Write(
            output,
            Array.Empty<Term>(),
            header.Type,
            (int)header.KeyPosition,
            ErlangPortableHash.Phash2,
            min,
            max
        );
        var repaired = new DetsV9File(output, false, runtime: runtime);
        foreach (Term value in Scan(source, header, runtime: runtime))
        {
            repaired.Insert([value], onlyNew: false);
        }
        repaired.Save(durable: true);
    }

    private static int Length(byte[] bytes, int position, int end)
    {
        if (position < 0 || end - position < DetsV9Layout.WordBytes)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        uint size = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(position));
        if (size < DetsV9Layout.WordBytes || size > end - position)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }

        return (int)size;
    }
}
