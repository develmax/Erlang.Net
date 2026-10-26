using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace BeamFixtureExport;

internal static class BeamChunkReader
{
    public static object Read(byte[] bytes)
    {
        if (bytes.Length < BeamFixtureWire.FormHeaderBytes || !bytes.AsSpan(0, BeamFixtureWire.IdentifierBytes).SequenceEqual(BeamFixtureWire.FormMarker) || !bytes.AsSpan(BeamFixtureWire.FormTypeOffset, BeamFixtureWire.IdentifierBytes).SequenceEqual(BeamFixtureWire.BeamMarker))
        {
            throw new InvalidDataException(FixtureExportDiagnostics.InvalidForm);
        }
        long formEnd = (long)BeamFixtureWire.FormLengthBaseBytes + BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(BeamFixtureWire.FormLengthOffset, BeamFixtureWire.IdentifierBytes));
        if (formEnd != bytes.Length)
        {
            throw new InvalidDataException(FixtureExportDiagnostics.FormLengthMismatch);
        }
        var chunks = new List<object>();
        int offset = BeamFixtureWire.FormHeaderBytes;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < BeamFixtureWire.ChunkHeaderBytes)
            {
                throw new InvalidDataException(FixtureExportDiagnostics.TruncatedHeader);
            }
            string id = Encoding.ASCII.GetString(bytes, offset, BeamFixtureWire.IdentifierBytes);
            uint size = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + BeamFixtureWire.ChunkLengthOffset, BeamFixtureWire.IdentifierBytes));
            long end = offset + (long)BeamFixtureWire.ChunkHeaderBytes + size;
            long paddedEnd = (end + BeamFixtureWire.AlignmentMask) & ~(long)BeamFixtureWire.AlignmentMask;
            if (paddedEnd > bytes.Length || size > int.MaxValue)
            {
                throw new InvalidDataException(FixtureExportDiagnostics.TruncatedPayload);
            }
            chunks.Add(new
            {
                Id = id,
                HeaderOffset = offset,
                PayloadBytes = size,
                PayloadSha256 = Convert.ToHexString(SHA256.HashData(bytes.AsSpan(offset + BeamFixtureWire.ChunkHeaderBytes, (int)size))),
                PaddingBytes = paddedEnd - end
            });
            offset = checked((int)paddedEnd);
        }

        return chunks;
    }
}
