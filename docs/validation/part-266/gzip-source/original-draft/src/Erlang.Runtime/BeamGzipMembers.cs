using System.Buffers.Binary;
using System.IO.Compression;

namespace Erlang;

internal static class BeamGzipMembers
{
    internal static byte[] Inflate(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty)
            throw new InvalidDataException();
        using var output = new MemoryStream();
        while (!input.IsEmpty)
        {
            int header = Header(input);
            int payload = BeamZlibMemberBoundary.MeasureDeflate(input[header..], int.MaxValue);
            int memberBytes = checked(header + payload + BeamGzipFormat.TrailerBytes);
            if (memberBytes > input.Length)
                throw new InvalidDataException();
            using var compressed = new MemoryStream(input[..memberBytes].ToArray());
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            gzip.CopyTo(output);
            input = input[memberBytes..];
        }

        return output.ToArray();
    }

    private static int Header(ReadOnlySpan<byte> input)
    {
        if (input.Length < BeamGzipFormat.HeaderBytes || input[0] != BeamGzipFormat.SignatureFirst || input[1] != BeamGzipFormat.SignatureSecond ||
            input[BeamGzipFormat.MethodOffset] != BeamGzipFormat.CompressionMethod || (input[BeamGzipFormat.FlagsOffset] & BeamGzipFormat.ReservedFlags) != 0)
            throw new InvalidDataException();
        int offset = BeamGzipFormat.HeaderBytes;
        byte flags = input[BeamGzipFormat.FlagsOffset];
        if ((flags & BeamGzipFormat.Extra) != 0)
        {
            if (input.Length - offset < BeamGzipFormat.ExtraLengthBytes)
                throw new InvalidDataException();
            int extra = BinaryPrimitives.ReadUInt16LittleEndian(input[offset..]);
            offset = checked(offset + BeamGzipFormat.ExtraLengthBytes + extra);
            if (offset > input.Length)
                throw new InvalidDataException();
        }
        if ((flags & BeamGzipFormat.Name) != 0)
            offset = CString(input, offset);
        if ((flags & BeamGzipFormat.Comment) != 0)
            offset = CString(input, offset);
        if ((flags & BeamGzipFormat.HeaderChecksum) != 0)
        {
            offset = checked(offset + BeamGzipFormat.HeaderChecksumBytes);
            if (offset > input.Length)
                throw new InvalidDataException();
        }

        return offset;
    }

    private static int CString(ReadOnlySpan<byte> input, int offset)
    {
        while (offset < input.Length)
            if (input[offset++] == BeamGzipFormat.Terminator)
                return offset;

        throw new InvalidDataException();
    }
}
