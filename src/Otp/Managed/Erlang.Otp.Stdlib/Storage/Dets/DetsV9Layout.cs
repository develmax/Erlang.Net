// Derived from Erlang/OTP 29.1.1 dets.hrl and dets_v9.erl (Apache-2.0).
// Copyright Ericsson AB 1996-2026. See retained upstream sources and licenses.
namespace Erlang;

internal static class DetsV9Layout
{
    internal const uint Cookie = 0x0abcdef;
    internal const uint Active = 0x12345678;
    internal const uint Free = 0x3abcdef;
    internal const uint EndFree = 12345;
    internal const int Version = 9;
    internal const int WordBytes = 4;
    internal const int FreelistOffset = 0;
    internal const int CookieOffset = 4;
    internal const int ClosedOffset = 8;
    internal const int TypeOffset = 12;
    internal const int VersionOffset = 16;
    internal const int MOffset = 20;
    internal const int NextOffset = 24;
    internal const int KeyPositionOffset = 28;
    internal const int ObjectCountOffset = 32;
    internal const int KeyCountOffset = 36;
    internal const int MinimumSlotsOffset = 40;
    internal const int MaximumSlotsOffset = 44;
    internal const int HashMethodOffset = 48;
    internal const int NOffset = 52;
    internal const int HeaderBytes = 312;
    internal const int DigestStart = 12;
    internal const int DigestOffset = 168;
    internal const int DigestBytes = 16;
    internal const int BaseOffset = 184;
    internal const int MetadataBytes = BaseOffset + WordBytes;
    internal const int CountersOffset = 56;
    internal const int MinimumCollectionExponent = 4;
    internal const int MaximumCollectionExponent = 31;
    internal const int SegmentArrayParts = 256;
    internal const int SegmentsPerPart = 512;
    internal const int SlotsPerSegment = 256;
    internal const int SlotBytes = 8;
    internal const int SegmentBytes = SlotsPerSegment * SlotBytes;
    internal const int PartBytes = SegmentsPerPart * WordBytes;
    internal const int LegacyBase = HeaderBytes + SegmentArrayParts * WordBytes;
    internal const int MinimumSlots = 256;
    internal const int MaximumSlots = SegmentArrayParts * SegmentsPerPart * SlotsPerSegment;
    internal const int ObjectHeaderBytes = 8;
    internal const int FreeHeaderBytes = 12;
    internal const int MaximumFreeRecordBytes = 4096;
    internal const long AddressSpaceBytes = 1L << 31;
    internal const int FreeListReserveBytes = 50000000;
    internal const int RepairAlignment = 16;
    internal const int LegacyHashRange = 0x3ffffff;
}
