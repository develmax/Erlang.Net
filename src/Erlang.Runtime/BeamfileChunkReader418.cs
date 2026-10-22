using System.Buffers.Binary;

namespace Erlang;

internal static class BeamfileChunkReader418
{
    internal static Term Read(Term input, Term identifier, CancellationToken cancellation)
    {
        // Native read_iff_list precedes binary validation, and accepts exactly
        // four proper byte cons cells; atoms/binary IDs are not converted.
        uint requested = ReadIdentifier(identifier);
        if (input is not BitString binary || !binary.IsBinary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        cancellation.ThrowIfCancellationRequested();
        ReadOnlySpan<byte> bytes = binary.ByteSpan;
        if (bytes.Length < BeamfileNativeNames418.ChunkHeaderBytes
            || BinaryPrimitives.ReadUInt32BigEndian(bytes) != BeamfileNativeNames418.FormId)
            return Term.A(BeamfileNativeNames418.Undefined);
        int formLength = BinaryPrimitives.ReadInt32BigEndian(bytes[BeamfileNativeNames418.WordBytes..]);
        if (formLength < BeamfileNativeNames418.WordBytes || formLength > bytes.Length - BeamfileNativeNames418.ChunkHeaderBytes)
            return Term.A(BeamfileNativeNames418.Undefined);
        // Outer trailing bytes are ignored; every byte inside the declared form
        // is scanned, including chunks after a matching one. Last match wins.
        int end = BeamfileNativeNames418.ChunkHeaderBytes + formLength;
        int position = BeamfileNativeNames418.ChunkHeaderBytes;
        if (BinaryPrimitives.ReadUInt32BigEndian(bytes[position..]) != BeamfileNativeNames418.BeamId)
            return Term.A(BeamfileNativeNames418.Undefined);
        position += BeamfileNativeNames418.WordBytes;
        int found = 0, foundLength = 0;
        while (position != end)
        {
            cancellation.ThrowIfCancellationRequested();
            if (end - position < BeamfileNativeNames418.ChunkHeaderBytes)
                return Term.A(BeamfileNativeNames418.Undefined);
            // C promotes signed Sint32 chunk_id to Uint (the VM word size).
            nuint actual = unchecked((nuint)(nint)BinaryPrimitives.ReadInt32BigEndian(bytes[position..]));
            int length = BinaryPrimitives.ReadInt32BigEndian(bytes[(position + BeamfileNativeNames418.WordBytes)..]);
            position += BeamfileNativeNames418.ChunkHeaderBytes;
            long storage = ((long)length + BeamfileNativeNames418.AlignmentMask) & ~((long)BeamfileNativeNames418.AlignmentMask);
            if (length < 0 || storage > end - position)
                return Term.A(BeamfileNativeNames418.Undefined);
            if (actual == (nuint)requested)
            {
                found = position;
                foundLength = length;
            }
            position += (int)storage;
        }
        cancellation.ThrowIfCancellationRequested();

        return foundLength == 0 ? Term.A(BeamfileNativeNames418.Undefined)
            : binary.SliceBits(found * BeamfileNativeNames418.BitsPerByte, foundLength * BeamfileNativeNames418.BitsPerByte);
    }

    private static uint ReadIdentifier(Term value)
    {
        uint result = 0;
        for (int index = 0; index < BeamfileNativeNames418.WordBytes; index++)
        {
            if (value is not Cons cell || cell.Head is not Integer octet || octet.Value < byte.MinValue || octet.Value > byte.MaxValue)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            result = (result << BeamfileNativeNames418.BitsPerByte) | (byte)octet.Value;
            value = cell.Tail;
        }
        if (value is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return result;
    }
}
