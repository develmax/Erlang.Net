using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamContextForward397Names;

namespace Erlang.Tests;

internal static class BeamContextForward397Bytes
{
    internal static byte[] Image(
        bool required = false,
        bool contextArgument = true,
        bool missingRoot = false,
        bool overwriteX = false,
        bool positionArgument = false,
        bool nativeEscape = false,
        bool cycle = false
    )
    {
        byte[] initial = contextArgument ? [LegacyStart, F(1), X(0), U(1), X(0)] : [];
        byte[] caller = [.. BeamOpcodeEdgeBytes.Start(
            1,
            2,
            2,
            1
        ), .. initial,
            .. (positionArgument ? new byte[] { SavePosition, X(0), X(0), U(1) } : Array.Empty<byte>()),
            Allocate, U(0), U(1), Call, U(1), F(4), Deallocate, U(0), Return];
        byte[] forward = cycle ? [Allocate, U(0), U(1), Call, U(1), F(4), Deallocate, U(0), Return]
            : [Allocate, U(1), U(1),
                .. (missingRoot ? Array.Empty<byte>() : new byte[] { InitY, List, U(1), Y(0) }),
                .. (overwriteX ? new byte[] { Move, A(8), X(0) } : Array.Empty<byte>()),
                .. (nativeEscape ? new byte[] { Deallocate, U(1), ExternalOnly, U(1), U(0) }
                    : new byte[] { Call, U(1), F(6), Deallocate, U(1), Return })];
        byte[] decoder = required ? [StartMatch, A(7), U(1), X(0), X(0)] : [LegacyStart, F(5), X(0), U(1), X(0)];
        byte[] image = BeamOpcodeEdgeBytes.Image(
            [
            .. caller, .. BeamOpcodeEdgeBytes.Start(
                3,
                4,
                3,
                1
            ), .. forward,
            .. BeamOpcodeEdgeBytes.Start(
                5,
                6,
                4,
                1
            ), .. decoder, Tail, X(0), X(0), U(1), Return
        ],
            labels: 7,
            functions: 3
        );
        if (!required)
            return image;
        string[] atoms = [BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run, BeamOpcodeEdgeNames.Middle,
            BeamOpcodeEdgeNames.Leaf, BeamOpcodeEdgeNames.Erlang, BeamOpcodeEdgeNames.Length,
            BeamBinaryProtocol390Names.ResumeAtomName, BeamOpcodeEdgeNames.Other];
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
