using System.Buffers.Binary;
using System.Text;
using Erlang;

namespace Erlang.Tests;

internal static class BeamMetadataModeledBytes
{
    internal static byte[] WithAttributes(byte[] input, Term? attributes) =>
        WithChunk(input, BeamPreparedHostNames.AttributesChunk, attributes is null ? null : ExternalTermFormat.Encode(attributes));

    internal static byte[] WithChunk(byte[] input, string chunkId, byte[]? payload)
    {
        using var output = new MemoryStream();
        output.Write(input.AsSpan(0, BeamPreparedHostNames.FormHeaderBytes));
        Span<byte> header = stackalloc byte[BeamPreparedHostNames.ChunkHeaderBytes];
        for (int offset = BeamPreparedHostNames.FormHeaderBytes; offset < input.Length;)
        {
            string id = Encoding.ASCII.GetString(input, offset, BeamPreparedHostNames.ChunkIdBytes);
            int length = BinaryPrimitives.ReadInt32BigEndian(input.AsSpan(offset + BeamPreparedHostNames.ChunkIdBytes));
            int padding = (BeamPreparedHostNames.AlignmentBytes - length % BeamPreparedHostNames.AlignmentBytes) % BeamPreparedHostNames.AlignmentBytes;
            if (id == chunkId)
            {
                if (payload is not null)
                {
                    header.Clear();
                    Encoding.ASCII.GetBytes(id, header);
                    BinaryPrimitives.WriteInt32BigEndian(header[BeamPreparedHostNames.ChunkIdBytes..], payload.Length);
                    output.Write(header);
                    output.Write(payload);
                    int tail = (BeamPreparedHostNames.AlignmentBytes - payload.Length % BeamPreparedHostNames.AlignmentBytes) % BeamPreparedHostNames.AlignmentBytes;
                    output.Write(new byte[tail]);
                }
            }
            else
            {
                output.Write(input.AsSpan(offset, BeamPreparedHostNames.ChunkHeaderBytes + length + padding));
            }
            offset += BeamPreparedHostNames.ChunkHeaderBytes + length + padding;
        }
        byte[] result = output.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(BeamPreparedHostNames.FormSizeOffset), result.Length - BeamPreparedHostNames.FormSizeBias);

        return result;
    }
}
