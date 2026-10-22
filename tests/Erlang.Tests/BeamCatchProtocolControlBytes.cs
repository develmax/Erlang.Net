using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamCatchProtocolControlNames;

namespace Erlang.Tests;

internal static class BeamCatchProtocolControlBytes
{
    internal static byte[] Positive(bool trim, bool readBlank = false)
    {
        int slots = trim ? 4 : 1;
        int tag = slots - 1;
        int retained = trim ? slots - 1 : slots;
        byte[] shift = trim ? [Trim, U(1), U(retained)] : [];

        return WireImage(
            [
            .. Start(
                1,
                2,
                2,
                1
            ),.. InitializeFrame(slots),Catch,Y(tag),F(3),
            .. shift,CallFun,A(UnsafeAtomIndex),U(0),X(0),Jump,F(3),Label,U(3),CatchEnd,Y(retained-1),
            .. (readBlank ? new byte[]{Move,Y(retained-1),X(0)} : []),
            Deallocate,U(retained),Return
        ],
            labels: 4
        );
    }

    internal static byte[] Negative(BeamCatchProtocolFault fault)
    {
        byte install = fault is BeamCatchProtocolFault.TryAsCatchEnd or BeamCatchProtocolFault.NormalTryHandlerEntry ? Try : Catch;
        byte[] body = fault switch
        {
            BeamCatchProtocolFault.CatchAsTryEnd => [TryEnd, Y(1)],
            BeamCatchProtocolFault.CatchAsTryCase => [TryCase, Y(1)],
            BeamCatchProtocolFault.TryAsCatchEnd => [CatchEnd, Y(1)],
            BeamCatchProtocolFault.WrongSlot => [CatchEnd, Y(0)],
            BeamCatchProtocolFault.TagEscape => [Move, Y(1), X(0)],
            BeamCatchProtocolFault.LiveTagReturn => [Deallocate, U(2), Return],
            BeamCatchProtocolFault.TrimDropsTag => [Trim, U(2), U(0)],
            BeamCatchProtocolFault.NormalTryHandlerEntry => [Jump, F(3)],
            BeamCatchProtocolFault.PendingRawEscape or BeamCatchProtocolFault.PendingHandlerOverwrite => [CallFun, A(UnsafeAtomIndex), U(0), X(0), CatchEnd, Y(1), Deallocate, U(2), Return],
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };
        byte[] exceptional = fault switch
        {
            BeamCatchProtocolFault.PendingRawEscape => [Move, X(2), X(0), CatchEnd, Y(1)],
            BeamCatchProtocolFault.PendingHandlerOverwrite => [Move, A(7), Y(1), CatchEnd, Y(1)],
            _ when install == Try => [TryCase, Y(1), Move, A(7), X(0)],
            _ => [CatchEnd, Y(1)]
        };

        return WireImage(
            [
            .. Start(
                1,
                2,
                2,
                1
            ),.. InitializeFrame(2),install,Y(1),F(3),
            .. body,Move,A(7),X(0),Jump,F(4),Label,U(3),.. exceptional,
            Label,U(4),Deallocate,U(2),Return
        ],
            labels: 5
        );
    }

    private static byte[] InitializeFrame(int slots)
        => [Allocate, U(slots), U(1), InitializeY, ArgumentList, U(slots), .. Enumerable.Range(0, slots).Select(Y)];

    private static byte[] WireImage(byte[] instructions, int labels)
    {
        byte[] image = BeamOpcodeEdgeBytes.Image(instructions, labels: labels);
        byte[]? atoms = null, code = null;
        for (int cursor = 12; cursor < image.Length;)
        {
            string name = Encoding.ASCII.GetString(image, cursor, 4);
            int bytes = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(cursor + 4, 4));
            if (name == BeamOpcodeEdgeNames.Atoms) atoms = image.AsSpan(cursor + 8, bytes).ToArray();
            if (name == BeamOpcodeEdgeNames.Code) code = image.AsSpan(cursor + 8, bytes).ToArray();
            cursor += 8 + ((bytes + 3) & ~3);
        }
        if (atoms is null || code is null || BinaryPrimitives.ReadInt32BigEndian(atoms) != UnsafeAtomIndex - 1)
            throw new InvalidDataException(Failure);
        byte[] unsafeName = Encoding.UTF8.GetBytes(Unsafe);
        byte[] extended = new byte[atoms.Length + 1 + unsafeName.Length];
        atoms.CopyTo(extended, 0);
        BinaryPrimitives.WriteInt32BigEndian(extended, UnsafeAtomIndex);
        extended[atoms.Length] = checked((byte)unsafeName.Length);
        unsafeName.CopyTo(extended, atoms.Length + 1);
        BinaryPrimitives.WriteInt32BigEndian(code.AsSpan(MaximumOpcodeOffset, 4), CallFun);
        image = BeamMetadataModeledBytes.WithChunk(image, BeamOpcodeEdgeNames.Atoms, extended);

        return BeamMetadataModeledBytes.WithChunk(image, BeamOpcodeEdgeNames.Code, code);
    }
}
