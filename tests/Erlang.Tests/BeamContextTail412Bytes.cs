using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamContextForward397Names;

namespace Erlang.Tests;

internal static class BeamContextTail412Bytes
{
    internal static byte[] Image(
        bool contextArgument = true,
        bool overwrite = false,
        bool duplicate = false,
        bool position = false,
        bool cycle = false
    )
    {
        byte[] caller = [.. Start(
            1,
            2,
            2,
            1
        ),
            .. (contextArgument ? new byte[] { LegacyStart, F(1), X(0), U(1), X(0) } : []),
            .. (position ? new byte[] { SavePosition, X(0), X(0), U(1) } : []),
            BeamContextTail412Names.Move, I(0), X(1), BeamContextTail412Names.LocalTail, U(2), F(4)];
        byte[] forward = [.. Start(
            3,
            4,
            3,
            2
        ),
            BeamContextTail412Names.Move, duplicate ? X(0) : X(1), X(2),
            BeamContextTail412Names.Move, I(0), X(1),
            .. (overwrite ? new byte[] { BeamContextTail412Names.Move, I(0), X(0) } : []),
            BeamContextTail412Names.LocalTail, U(cycle ? 2 : 3), F(cycle ? 4 : 6)];
        byte[] decoder = [.. Start(
            5,
            6,
            4,
            3
        ),
            StartMatch, A(7), U(3), X(0), X(0), Tail, X(0), X(0), U(3), Return];
        byte[] image = BeamOpcodeEdgeBytes.Image([.. caller, .. forward, .. decoder], labels: 7, functions: 3);

        string[] atoms = [
                BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run, BeamOpcodeEdgeNames.Middle,
                BeamOpcodeEdgeNames.Leaf, BeamOpcodeEdgeNames.Erlang, BeamOpcodeEdgeNames.Length,
                BeamBinaryProtocol390Names.ResumeAtomName, BeamOpcodeEdgeNames.Other
            ];
        using var payload = new MemoryStream();
        Span<byte> count = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(count, atoms.Length);
        payload.Write(count);
        foreach (string atom in atoms)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(atom);
            payload.WriteByte(checked((byte)bytes.Length));
            payload.Write(bytes);
        }

        return BeamMetadataModeledBytes.WithChunk(image, BeamOpcodeEdgeNames.Atoms, payload.ToArray());
    }
}
