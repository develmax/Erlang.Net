using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamRaiseEmpty412Names;

namespace Erlang.Tests;

internal static class BeamRaiseEmpty412Bytes
{
    internal static byte[] Image(BeamRaiseEmpty412Shape shape)
    {
        bool unknown = shape == BeamRaiseEmpty412Shape.UnknownClass;
        byte[] trace = shape == BeamRaiseEmpty412Shape.NonNil ? [Move, A(MarkerAtom), X(2)]
            : shape == BeamRaiseEmpty412Shape.Bypass ? [Move, A(MarkerAtom), X(2), Jump, F(4), Move, A(0), X(2), Label, U(4)]
            : [Move, A(0), X(2)];
        byte[] clobber = shape == BeamRaiseEmpty412Shape.Clobber ? [Move, A(MarkerAtom), X(2)] : [];
        byte[] exceptionClass = unknown ? [] : [Move,
            A(shape is BeamRaiseEmpty412Shape.InvalidClass or BeamRaiseEmpty412Shape.OrdinaryBadarg ? MarkerAtom : ThrowAtom), X(0)];
        byte[] ordinary = shape == BeamRaiseEmpty412Shape.OrdinaryBadarg ? [TryEnd, Y(1), Deallocate, U(2), Return] : [];
        byte[] image = BeamOpcodeEdgeBytes.Image(
            [
            .. Start(
                1,
                2,
                2,
                unknown ? 1 : 0
            ), Allocate, U(2), U(unknown ? 1 : 0), Move, A(0), Y(0), Try, Y(1), F(3),
            Move, A(MarkerAtom), X(1), .. trace, InitY, 0x17, U(1), Y(0), .. clobber, .. exceptionClass,
            Line, U(0), ExternalCall, U(3), U(0), .. ordinary,
            Label, U(3), TryCase, Y(1), Move, X(1), X(0), Deallocate, U(2), Return
        ],
            labels: 5,
            arity: unknown ? 1 : 0,
            importFunction: RaiseFunction,
            importArity: 3
        );
        string[] atoms = [BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run, ThrowClass, ErrorClass,
            BeamOpcodeEdgeNames.Erlang, RaiseFunction, BeamOpcodeEdgeNames.Marker, BeamOpcodeEdgeNames.Other];
        using var table = new MemoryStream();
        byte[] count = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(count, atoms.Length);
        table.Write(count);
        foreach (string atom in atoms)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(atom);
            table.WriteByte(checked((byte)bytes.Length));
            table.Write(bytes);
        }

        return BeamMetadataModeledBytes.WithChunk(image, AtomChunk, table.ToArray());
    }
}
