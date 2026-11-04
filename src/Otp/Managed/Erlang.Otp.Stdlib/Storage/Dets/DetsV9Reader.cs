// Derived from Erlang/OTP 29.1.1 dets_v9.erl (Apache-2.0).
// Copyright Ericsson AB 2001-2026. Upstream notices are retained in Otp/dets_v9.erl.
using System.Buffers.Binary;

namespace Erlang;

/// <summary>Reads v9(a-d) slot collections without executing Erlang or an external VM.</summary>
internal sealed class DetsV9Reader
{
    private readonly Stream stream;
    private readonly EtfDecodeOptions decodeOptions;
    private readonly long dataEnd;
    private readonly ProcessRuntime? runtime;
    private readonly Dictionary<uint, long> segments = new();

    public DetsV9Reader(
        Stream stream,
        EtfDecodeOptions? decodeOptions = null,
        bool allowDirty = false,
        ProcessRuntime? runtime = null
    )
    {
        this.stream = stream;
        this.runtime = runtime;
        this.decodeOptions = decodeOptions ?? new EtfDecodeOptions();
        Header = DetsV9Header.Read(stream);
        if (!allowDirty)
        {
            Header.Validate();
            Header.ValidateAddressing();
        }
        dataEnd = allowDirty ? stream.Length : Header.Freelist;
    }

    public DetsV9Header Header { get; }

    public IReadOnlyList<Term> ReadSlot(uint slot)
    {
        if (slot >= Header.Next)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }
        long position = SlotPosition(slot);
        uint size = Word(position);
        uint pointer = Word(position + DetsV9Layout.WordBytes);
        if (pointer == 0)
        {
            if (size != 0)
            {
                throw new DetsFileException(DetsFileReasons.BadObject);
            }

            return Array.Empty<Term>();
        }
        if (size < DetsV9Layout.ObjectHeaderBytes || pointer < Header.Base
            || (long)pointer + size > dataEnd)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        if (Word(pointer) != size || Word(pointer + DetsV9Layout.WordBytes) != DetsV9Layout.Active)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        long end = (long)pointer + size;
        long cursor = pointer + DetsV9Layout.ObjectHeaderBytes;
        var result = new List<Term>();
        while (cursor < end)
        {
            long groupEnd = end;
            if (Header.Type != DetsFileType.Set)
            {
                uint groupBytes = Word(cursor);
                if (groupBytes < DetsV9Layout.WordBytes || groupBytes > end - cursor)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                groupEnd = cursor + groupBytes;
                cursor += DetsV9Layout.WordBytes;
            }
            int countBefore = result.Count;
            do
            {
                if (groupEnd - cursor < DetsV9Layout.WordBytes)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                uint objectBytes = Word(cursor);
                if (objectBytes <= DetsV9Layout.WordBytes || objectBytes > groupEnd - cursor
                    || objectBytes > int.MaxValue)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                var encoded = new byte[checked((int)objectBytes - DetsV9Layout.WordBytes)];
                stream.Position = cursor + DetsV9Layout.WordBytes;
                stream.ReadExactly(encoded);
                Term decoded;
                try
                {
                    decoded = runtime is null ? ExternalTermFormat.DecodeOne(encoded, decodeOptions, encoded.Length).Term
                        : runtime.ImportExternalTerm(encoded, decodeOptions, encoded.Length).Term;
                }
                catch (ErlangException)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                if (decoded is not TupleTerm tuple || tuple.Items.Count < Header.KeyPosition)
                {
                    throw new DetsFileException(DetsFileReasons.BadObject);
                }
                result.Add(decoded);
                cursor += objectBytes;
                if (Header.Type == DetsFileType.Set)
                {
                    break;
                }
                if (result.Count > countBefore + 1)
                {
                    var first = (TupleTerm)result[countBefore];
                    if (!first.Items[(int)Header.KeyPosition - 1].Equals(tuple.Items[(int)Header.KeyPosition - 1]))
                    {
                        throw new DetsFileException(DetsFileReasons.BadObject);
                    }
                }
            }
            while (cursor < groupEnd);
            if (Header.Type != DetsFileType.Set && (cursor != groupEnd || result.Count == countBefore))
            {
                throw new DetsFileException(DetsFileReasons.BadObject);
            }
        }
        if (cursor != end)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }

        return result.AsReadOnly();
    }

    public IEnumerable<Term> ReadObjects()
    {
        for (uint slot = 0; slot < Header.Next; slot++)
        {
            foreach (Term item in ReadSlot(slot))
            {
                yield return item;
            }
        }
    }

    public IReadOnlyList<(int Exponent, long Address)> ReadFreeBlocks()
    {
        var blocks = new List<(int Exponent, long Address)>();
        long position = Header.Freelist;
        uint previousPosition = 0;
        long previousAddress = -1;
        while (position <= Header.FileLength - DetsV9Layout.FreeHeaderBytes - DetsV9Layout.WordBytes)
        {
            uint bytes = Word(position);
            uint status = Word(position + DetsV9Layout.WordBytes);
            uint listPosition = Word(position + DetsV9Layout.ObjectHeaderBytes);
            if (bytes < DetsV9Layout.FreeHeaderBytes || bytes > DetsV9Layout.MaximumFreeRecordBytes
                || bytes % DetsV9Layout.WordBytes != 0 || position + bytes > Header.FileLength - DetsV9Layout.WordBytes
                || status != DetsV9Layout.Free)
            {
                throw new DetsFileException(DetsFileReasons.BadFreelists);
            }
            if (listPosition == DetsV9Layout.EndFree)
            {
                if (bytes != DetsV9Layout.FreeHeaderBytes || position + bytes != Header.FileLength - DetsV9Layout.WordBytes)
                {
                    throw new DetsFileException(DetsFileReasons.BadFreelists);
                }

                return blocks.AsReadOnly();
            }
            if (listPosition is < 1 or > DetsV9Layout.MaximumCollectionExponent + 1 || listPosition < previousPosition)
            {
                throw new DetsFileException(DetsFileReasons.BadFreelists);
            }
            if (listPosition != previousPosition)
            {
                previousAddress = -1;
            }
            int exponent = (int)listPosition - 1;
            for (long offset = DetsV9Layout.FreeHeaderBytes; offset < bytes; offset += DetsV9Layout.WordBytes)
            {
                long address = Word(position + offset);
                if (address <= previousAddress || address < Header.Base
                    || (address - Header.Base) % (1L << exponent) != 0
                    || address - Header.Base + (1L << exponent) > DetsV9Layout.AddressSpaceBytes)
                {
                    throw new DetsFileException(DetsFileReasons.BadFreelists);
                }
                blocks.Add((exponent, address));
                previousAddress = address;
            }
            previousPosition = listPosition;
            position += bytes;
        }

        throw new DetsFileException(DetsFileReasons.BadFreelists);
    }

    internal long SlotPosition(uint slot)
    {
        uint segment = slot / DetsV9Layout.SlotsPerSegment;
        if (!segments.TryGetValue(segment, out long pointer))
        {
            uint part = Word(DetsV9Layout.HeaderBytes + segment / DetsV9Layout.SegmentsPerPart * DetsV9Layout.WordBytes);
            if (part < DetsV9Layout.LegacyBase || (long)part + DetsV9Layout.PartBytes > dataEnd)
            {
                throw new DetsFileException(DetsFileReasons.BadObject);
            }
            pointer = Word(part + segment % DetsV9Layout.SegmentsPerPart * DetsV9Layout.WordBytes);
            if (pointer < DetsV9Layout.LegacyBase || pointer + DetsV9Layout.SegmentBytes > dataEnd)
            {
                throw new DetsFileException(DetsFileReasons.BadObject);
            }
            segments.Add(segment, pointer);
        }

        return pointer + slot % DetsV9Layout.SlotsPerSegment * DetsV9Layout.SlotBytes;
    }

    private uint Word(long position)
    {
        if (position < 0 || position > Header.FileLength - DetsV9Layout.WordBytes)
        {
            throw new DetsFileException(DetsFileReasons.BadObject);
        }
        Span<byte> bytes = stackalloc byte[DetsV9Layout.WordBytes];
        stream.Position = position;
        stream.ReadExactly(bytes);

        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}
