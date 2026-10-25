using System.Buffers.Binary;

namespace Erlang;

internal static class BeamTypeTableReader
{
    internal static BeamTypeTable Read(BeamStructure structure)
    {
        BeamChunk? chunk = structure.Chunks.LastOrDefault(value => value.Id == BeamTypeWire.Chunk);
        if (chunk is null || chunk.Payload.Length == 0)
            return Fallback(null);
        var cursor = new BeamCursor(chunk.Payload);
        int version = cursor.ReadInt32();
        if (version is not BeamTypeWire.CurrentVersion and not BeamTypeWire.PreviousVersion)
            return Fallback(version);
        int count = cursor.ReadInt32();
        Require(count >= 1 && count < BeamTypeWire.NativeAllocationSanityBytes / BeamTypeWire.NativeEntryBytes64
            && count <= cursor.Remaining / BeamTypeWire.TypeFlagBytes);
        var entries = new BeamTypeEntry[count];
        for (int index = 0; index < count; index++)
        {
            byte upper = cursor.ReadByte();
            byte lower = cursor.ReadByte();
            if (version == BeamTypeWire.PreviousVersion)
            {
                int metadata = (upper << BeamTypeWire.MetadataVersionShift) & BeamTypeWire.MetadataUpperByteMask;
                int union = upper & BeamTypeWire.PreviousUpperUnionMask;
                if (union == BeamTypeWire.PreviousUpperUnionMask)
                    union = BeamTypeWire.PreviousUpperAny;
                upper = (byte)(metadata | union);
            }
            int flags = (upper << BeamTypeWire.ByteBits) | lower;
            Require(flags != 0);
            int types = flags & BeamTypeWire.Any;
            int metadataFlags = flags & BeamTypeWire.MetadataMask;
            long minimum = BeamTypeWire.DefaultMinimum;
            long maximum = BeamTypeWire.DefaultMaximum;
            byte unit = 1;
            if ((metadataFlags & BeamTypeWire.LowerBound) != 0)
            {
                Require((types & (BeamTypeWire.Float | BeamTypeWire.Integer)) != 0);
                minimum = BinaryPrimitives.ReadInt64BigEndian(cursor.Read(BeamTypeWire.BoundBytes).Span);
            }
            if ((metadataFlags & BeamTypeWire.UpperBound) != 0)
            {
                Require((types & (BeamTypeWire.Float | BeamTypeWire.Integer)) != 0);
                maximum = BinaryPrimitives.ReadInt64BigEndian(cursor.Read(BeamTypeWire.BoundBytes).Span);
            }
            if ((metadataFlags & BeamTypeWire.Unit) != 0)
            {
                Require((types & BeamTypeWire.BitString) != 0);
                unit = unchecked((byte)(cursor.ReadByte() + 1));
            }
            entries[index] = new(
                types,
                metadataFlags,
                minimum,
                maximum,
                unit
            );
        }
        Require(entries[0].TypeUnion == BeamTypeWire.Any && entries[0].Minimum > entries[0].Maximum);

        return new(false, version, Array.AsReadOnly(entries));
    }

    private static BeamTypeTable Fallback(int? version) => new(
        true,
        version,
        Array.AsReadOnly(new[]
    {
        new BeamTypeEntry(
            BeamTypeWire.Any,
            0,
            BeamTypeWire.DefaultMinimum,
            BeamTypeWire.DefaultMaximum,
            1
        )
    })
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(BeamLinkDiagnostics.TypeTable);
    }
}
