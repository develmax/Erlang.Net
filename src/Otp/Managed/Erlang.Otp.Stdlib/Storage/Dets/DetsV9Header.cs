// Derived from Erlang/OTP 29.1.1 dets_v9.erl (Apache-2.0).
// Copyright Ericsson AB 2001-2026. Upstream notices are retained in Otp/dets_v9.erl.
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Erlang;

/// <summary>The on-disk v9 header, independent of a running DETS server.</summary>
internal sealed class DetsV9Header
{
    private readonly uint[] counters;

    private DetsV9Header(byte[] bytes, long fileLength, uint trailer)
    {
        Freelist = Read(bytes, DetsV9Layout.FreelistOffset);
        Cookie = Read(bytes, DetsV9Layout.CookieOffset);
        ClosedProperly = Read(bytes, DetsV9Layout.ClosedOffset);
        TypeCode = Read(bytes, DetsV9Layout.TypeOffset);
        Version = Read(bytes, DetsV9Layout.VersionOffset);
        M = Read(bytes, DetsV9Layout.MOffset);
        Next = Read(bytes, DetsV9Layout.NextOffset);
        KeyPosition = Read(bytes, DetsV9Layout.KeyPositionOffset);
        ObjectCount = Read(bytes, DetsV9Layout.ObjectCountOffset);
        KeyCount = Read(bytes, DetsV9Layout.KeyCountOffset);
        MinimumSlots = Read(bytes, DetsV9Layout.MinimumSlotsOffset);
        MaximumSlots = Read(bytes, DetsV9Layout.MaximumSlotsOffset);
        HashMethod = Read(bytes, DetsV9Layout.HashMethodOffset);
        N = Read(bytes, DetsV9Layout.NOffset);
        StoredBase = Read(bytes, DetsV9Layout.BaseOffset);
        Base = StoredBase == 0 ? DetsV9Layout.LegacyBase : StoredBase;
        FileLength = fileLength;
        Trailer = trailer;
        counters = new uint[DetsV9Layout.MaximumCollectionExponent - DetsV9Layout.MinimumCollectionExponent + 1];
        for (int index = 0; index < counters.Length; index++)
        {
            counters[index] = Read(bytes, DetsV9Layout.CountersOffset + index * DetsV9Layout.WordBytes);
        }
        ReadOnlySpan<byte> digest = bytes.AsSpan(DetsV9Layout.DigestOffset, DetsV9Layout.DigestBytes);
        HasDigest = digest.IndexOfAnyExcept((byte)0) >= 0;
        DigestValid = !HasDigest || CryptographicOperations.FixedTimeEquals(
            digest,
            MD5.HashData(bytes.AsSpan(DetsV9Layout.DigestStart, DetsV9Layout.DigestOffset - DetsV9Layout.DigestStart))
        );
    }

    public uint Freelist { get; }
    public uint Cookie { get; }
    public uint ClosedProperly { get; }
    public uint TypeCode { get; }
    public DetsFileType Type => (DetsFileType)TypeCode;
    public uint Version { get; }
    public uint M { get; }
    public uint N { get; }
    public uint Next { get; }
    public uint KeyPosition { get; }
    public uint ObjectCount { get; }
    public uint KeyCount { get; }
    public uint MinimumSlots { get; }
    public uint MaximumSlots { get; }
    public uint HashMethod { get; }
    public uint StoredBase { get; }
    public long Base { get; }
    public long FileLength { get; }
    public uint Trailer { get; }
    public bool HasDigest { get; }
    public bool DigestValid { get; }
    public IReadOnlyList<uint> CollectionCounters => Array.AsReadOnly(counters);

    public static DetsV9Header Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException(DetsFileReasons.InvalidStream, nameof(stream));
        }
        if (stream.Length < DetsV9Layout.MetadataBytes)
        {
            throw new DetsFileException(DetsFileReasons.NotDets);
        }
        var bytes = new byte[DetsV9Layout.HeaderBytes];
        stream.Position = 0;
        stream.ReadExactly(bytes.AsSpan(0, DetsV9Layout.MetadataBytes));
        Span<byte> tail = stackalloc byte[DetsV9Layout.WordBytes];
        stream.Position = stream.Length - tail.Length;
        stream.ReadExactly(tail);

        return new DetsV9Header(bytes, stream.Length, BinaryPrimitives.ReadUInt32BigEndian(tail));
    }

    /// <summary>Preserves the check ordering of dets_v9:check_file_header/2.</summary>
    public void Validate()
    {
        if (Cookie != DetsV9Layout.Cookie)
        {
            throw new DetsFileException(DetsFileReasons.NotDets);
        }
        if (TypeCode is < (uint)DetsFileType.Set or > (uint)DetsFileType.DuplicateBag)
        {
            throw new DetsFileException(DetsFileReasons.BadType);
        }
        if (Version != DetsV9Layout.Version)
        {
            throw new DetsFileException(DetsFileReasons.BadVersion);
        }
        if (!DigestValid)
        {
            throw new DetsFileException(DetsFileReasons.NotDets);
        }
        if ((long)Trailer + StoredBase != FileLength)
        {
            throw new DetsFileException(DetsFileReasons.NotClosed);
        }
        if (HashMethod > 1)
        {
            throw new DetsFileException(DetsFileReasons.BadHash);
        }
        if (ClosedProperly == 0)
        {
            throw new DetsFileException(DetsFileReasons.NotClosed);
        }
        if (ClosedProperly != 1)
        {
            throw new DetsFileException(DetsFileReasons.NotDets);
        }
    }

    internal void ValidateAddressing()
    {
        if (M < DetsV9Layout.MinimumSlots || M > DetsV9Layout.MaximumSlots || (M & (M - 1)) != 0
            || N > M || N % DetsV9Layout.SlotsPerSegment != 0 || Next != M + N
            || Next > DetsV9Layout.MaximumSlots || KeyPosition == 0 || KeyPosition > int.MaxValue
            || Base < DetsV9Layout.LegacyBase || Base > Freelist || Freelist > FileLength - DetsV9Layout.FreeHeaderBytes
            || MinimumSlots < DetsV9Layout.MinimumSlots || MinimumSlots > Next || MaximumSlots < Next
            || MaximumSlots > DetsV9Layout.MaximumSlots || KeyCount > ObjectCount)
        {
            throw new DetsFileException(DetsFileReasons.NotDets);
        }
    }

    private static uint Read(byte[] bytes, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, DetsV9Layout.WordBytes));
}
