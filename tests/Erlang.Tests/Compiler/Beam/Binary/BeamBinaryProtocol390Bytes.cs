using System.Buffers.Binary;
using System.Text;
using Erlang;
using static Erlang.Tests.BeamBinaryHostBytes;
using static Erlang.Tests.BeamBinaryProtocol390Names;

namespace Erlang.Tests;

internal static class BeamBinaryProtocol390Bytes
{
    internal static byte[] PositionRoundtripImage()
        => BeamBinaryHostBytes.Image(Join(
            Start(1, 0, 0),
            Instruction(Allocate, U(2), U(1)),
            Instruction(InitializeY, [ArgumentList, .. U(2), BeamOpcodeEdgeBytes.Y(0), BeamOpcodeEdgeBytes.Y(1)]),
            Instruction(
                SavePosition,
                X(0),
                [BeamOpcodeEdgeBytes.Y(1)],
                U(1)
            ),
            Instruction(Trim, U(1), U(1)),
            Instruction(
                ExtractBinary,
                F(3),
                X(0),
                U(1),
                [BeamOpcodeEdgeBytes.I(1)],
                U(8),
                U(0),
                X(1)
            ),
            Instruction(RestorePosition, X(0), [BeamOpcodeEdgeBytes.Y(0)]),
            Instruction(
                ExtractTail,
                X(0),
                X(0),
                U(1)
            ),
            Instruction(Deallocate, U(1)),
            [Return, Label, .. U(3)],
            Instruction(Deallocate, U(1)),
            Instruction(Move, A(7), X(0)),
            [Return]
        ));

    internal static byte[] PositionEscapeImage(bool native)
        => BeamBinaryHostBytes.Image(Join(
            Start(1, 0, 0),
            Instruction(
                SavePosition,
                X(0),
                X(0),
                U(1)
            ),
            native ? Instruction(ExternalTail, U(1), U(0)) : [Return]
        ));

    internal static byte[] ForgedPositionImage()
        => BeamBinaryHostBytes.Image(Join(
            Start(2, 0, 0),
            Instruction(RestorePosition, X(0), X(1)),
            Instruction(
                ExtractTail,
                X(0),
                X(0),
                U(1)
            ),
            [Return]
        ), arity: 2);

    internal static byte[] LocalResumeImage(bool contextArgument)
    {
        byte[] image = BeamBinaryHostBytes.Image(
            Join(
                contextArgument ? Start(1, 0, 0) : [],
                Instruction(CallOnly, U(1), F(4)),
                BeamOpcodeEdgeBytes.Start(
                    3,
                    4,
                    3,
                    1
                ),
                Instruction(
                    BeamBinaryHostNames.StartOpcode,
                    A(ResumeAtom),
                    U(1),
                    X(0),
                    X(0)
                ),
                Instruction(
                    ExtractTail,
                    X(0),
                    X(0),
                    U(1)
                ),
                [Return]
            ),
            labels: 5
        );
        byte[] code = Chunk(image, BeamBinaryHostNames.CodeChunk);
        BinaryPrimitives.WriteInt32BigEndian(code.AsSpan(CodeFunctionCountOffset), 2);
        image = BeamMetadataModeledBytes.WithChunk(image, BeamBinaryHostNames.CodeChunk, code);
        byte[] atoms = Chunk(image, BeamBinaryHostNames.Atoms);
        using var expanded = new MemoryStream();
        expanded.Write(atoms);
        byte[] atom = Encoding.UTF8.GetBytes(ResumeAtomName);
        expanded.WriteByte(checked((byte)atom.Length));
        expanded.Write(atom);
        byte[] payload = expanded.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(payload, ResumeAtom);

        return BeamMetadataModeledBytes.WithChunk(image, BeamBinaryHostNames.Atoms, payload);
    }

    internal static byte[] UnitTwoImage()
        => BeamBinaryHostBytes.Image(Join(
            Start(1, 0, 0),
            Instruction(
                BeamBinaryHostNames.MatchOpcode,
                F(3),
                X(0),
                [ArgumentList, .. U(11), .. A(BeamBinaryHostNames.EnsureAtom), .. U(8), .. U(2),
                    .. A(BeamBinaryHostNames.EqualAtom), .. A(0), .. U(8), .. U(0),
                    .. A(BeamBinaryHostNames.GetTailAtom), .. U(1), .. U(2), .. X(0)]
            ),
            [Return, Label, .. U(3)],
            Instruction(Move, A(7), X(0)),
            [Return]
        ));

    internal static byte[] LiteralImage(BitString value)
    {
        byte[] image = BeamBinaryHostBytes.Image(
            Join(
                Instruction(
                    BeamBinaryHostNames.StartOpcode,
                    A(BeamBinaryHostNames.NoFailAtom),
                    U(0),
                    [Literal, .. U(0)],
                    X(0)
                ),
                Instruction(
                    ExtractTail,
                    X(0),
                    X(0),
                    U(1)
                ),
                [Return]
            ),
            arity: 0
        );
        byte[] literal = ExternalTermFormat.Encode(value);
        using var table = new MemoryStream();
        Write32(table, 0);
        Write32(table, 1);
        Write32(table, literal.Length);
        table.Write(literal);
        using var output = new MemoryStream();
        output.Write(image);
        output.Write(Encoding.ASCII.GetBytes(LiteralChunk));
        Write32(output, checked((int)table.Length));
        output.Write(table.ToArray());
        output.Write(new byte[(Alignment - table.Length % Alignment) % Alignment]);
        byte[] result = output.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(FormSizeOffset), result.Length - FormSizeBias);

        return result;
    }

    private static byte[] Chunk(byte[] image, string name)
    {
        for (int cursor = FormBytes; cursor < image.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(cursor + AtomCountBytes));
            if (Encoding.ASCII.GetString(image, cursor, AtomCountBytes) == name)
                return image.AsSpan(cursor + ChunkHeaderBytes, length).ToArray();
            cursor += ChunkHeaderBytes + ((length + Alignment - 1) & ~(Alignment - 1));
        }
        throw new InvalidDataException(Failure);
    }

    private static void Write32(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }
}
