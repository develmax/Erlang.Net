using System.Buffers.Binary;
using System.Text;

namespace Erlang.Compiler;

internal static class BeamContainerWriter
{
    private const int ChunkAlignment = 4;

    internal static byte[] Write(IEnumerable<(string Id, byte[] Payload)> chunks)
    {
        using var body = new MemoryStream();
        body.Write("BEAM"u8);
        foreach (var (id, payload) in chunks)
        {
            body.Write(Encoding.ASCII.GetBytes(id));
            Word(body, payload.Length);
            body.Write(payload);
            int remainder = payload.Length % ChunkAlignment;
            if (remainder != 0)
            {
                body.Write(new byte[ChunkAlignment - remainder]);
            }
        }

        using var result = new MemoryStream();
        result.Write("FOR1"u8);
        Word(result, checked((int)body.Length));
        body.WriteTo(result);

        return result.ToArray();
    }

    internal static void Word(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }
}
