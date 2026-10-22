using System.Buffers.Binary;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamMissingHeaderLineBytes
{
    internal static byte[] Image()
    {
        // Synthetic protocol image: run has header line10; middle has no header
        // and later explicitly installs line12; leaf has neither header nor body line.
        byte[] instructions = [
            1, U(1), 153, U(1), 2, A(1), A(2), U(0), 1, U(2),
            64, A(7), X(0), 19,
            .. Start(
                3,
                4,
                3,
                0
            ), 64, A(8), X(0), 153, U(2), 19,
            .. Start(
                5,
                6,
                4,
                0
            ), 64, A(7), X(0), 19
        ];
        byte[] input = BeamOpcodeEdgeBytes.Image(
            instructions,
            labels: 7,
            functions: 3,
            arity: 0
        );
        using var line = new MemoryStream();
        Span<byte> word = stackalloc byte[4];
        foreach (int value in new[] { 0, 0, 2, 2, 0 })
        {
            BinaryPrimitives.WriteInt32BigEndian(word, value);
            line.Write(word);
        }
        line.WriteByte(I(10));
        line.WriteByte(I(12));
        // WithChunk is replacement-only; Image removed Line, so append a real chunk.
        using var output = new MemoryStream();
        output.Write(input);
        output.Write("Line"u8);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, checked((int)line.Length));
        output.Write(length);
        output.Write(line.ToArray());
        output.Write(new byte[(4 - line.Length % 4) % 4]);
        byte[] result = output.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(4), result.Length - 8);

        return result;
    }
}
