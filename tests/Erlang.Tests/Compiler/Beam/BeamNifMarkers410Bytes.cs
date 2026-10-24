using System.Buffers.Binary;

namespace Erlang.Tests;

internal static class BeamNifMarkers410Bytes
{
    // Wire controls only; no original OTP acceptance claim for synthetic images.
    internal static byte[] Image(int variant)
    {
        byte[] ordinary = [64, BeamOpcodeEdgeBytes.I(2), BeamOpcodeEdgeBytes.X(0), 19];
        byte[] run = variant switch
        {
            0 => ordinary,
            1 => [BeamNifMarkers410Names.Marker, .. ordinary],
            2 => [64, BeamOpcodeEdgeBytes.I(1), BeamOpcodeEdgeBytes.X(0), BeamNifMarkers410Names.Marker, .. ordinary],
            // Unreachable marker after a real admitted error/1 tail call.
            3 => [64, BeamOpcodeEdgeBytes.I(2), BeamOpcodeEdgeBytes.X(0), 78, BeamOpcodeEdgeBytes.U(1), BeamOpcodeEdgeBytes.U(0), BeamNifMarkers410Names.Marker],
            4 => [BeamNifMarkers410Names.Marker, BeamNifMarkers410Names.Marker, .. ordinary],
            5 => [6, BeamOpcodeEdgeBytes.U(0), BeamOpcodeEdgeBytes.F(4)],
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
        byte[] middle = variant == 5
            ? [BeamNifMarkers410Names.Marker, 64, BeamOpcodeEdgeBytes.I(3), BeamOpcodeEdgeBytes.X(0), 19]
            : [64, BeamOpcodeEdgeBytes.I(3), BeamOpcodeEdgeBytes.X(0), 19];
        byte[] instructions = [.. BeamOpcodeEdgeBytes.Start(
            1,
            2,
            2,
            0
        ), .. run,
            .. BeamOpcodeEdgeBytes.Start(
                3,
                4,
                3,
                0
            ), .. middle];
        byte[] image = BeamOpcodeEdgeBytes.Image(
            instructions,
            labels: 5,
            functions: 2,
            arity: 0,
            importFunction: BeamNifMarkers410Names.ErrorFunction,
            importArity: 1
        );
        using var code = new MemoryStream();
        Span<byte> word = stackalloc byte[4];
        foreach (int value in new[] { 16, 0, (int)BeamNifMarkers410Names.Marker, 5, 2 })
        {
            BinaryPrimitives.WriteInt32BigEndian(word, value);
            code.Write(word);
        }
        code.Write(instructions);
        code.WriteByte(3);

        return BeamMetadataModeledBytes.WithChunk(image, BeamOpcodeEdgeNames.Code, code.ToArray());
    }
}
