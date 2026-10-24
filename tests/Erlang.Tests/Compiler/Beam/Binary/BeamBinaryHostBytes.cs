using System.Buffers.Binary;
using System.Text;

namespace Erlang.Tests;

internal static class BeamBinaryHostBytes
{
    internal static byte[] U(int value) => Compact(value, 0);

    internal static byte[] A(int value) => Compact(value, 2);

    internal static byte[] X(int value) => Compact(value, 3);

    internal static byte[] F(int value) => Compact(value, 5);

    internal static byte[] Join(params byte[][] values) => values.SelectMany(value => value).ToArray();

    internal static byte[] Instruction(byte opcode, params byte[][] values) => [opcode, .. Join(values)];

    internal static byte[] Image(
        byte[]? body = null,
        int arity = 1,
        byte[]? strings = null,
        int labels = 4
    )
    {
        byte[] image = BeamOpcodeEdgeBytes.Image(
            [.. BeamOpcodeEdgeBytes.Start(
                1,
                2,
                2,
                arity
            ),
            .. (body ?? new byte[] { 64, BeamOpcodeEdgeBytes.A(7), BeamOpcodeEdgeBytes.X(0), 19 })],
            labels: labels,
            arity: arity
        );
        using var atoms = new MemoryStream();
        string[] names = [BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run, BeamOpcodeEdgeNames.Middle, BeamOpcodeEdgeNames.Leaf,
            BeamOpcodeEdgeNames.Erlang, BeamOpcodeEdgeNames.Length, BeamOpcodeEdgeNames.Marker, BeamOpcodeEdgeNames.Other, .. BeamBinaryHostNames.ExtraAtoms];
        Write32(atoms, names.Length);
        foreach (string name in names)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(name);
            atoms.WriteByte(checked((byte)bytes.Length));
            atoms.Write(bytes);
        }
        image = SetChunk(image, BeamBinaryHostNames.Atoms, atoms.ToArray());
        image = SetChunk(image, BeamBinaryHostNames.Strings, strings ?? new byte[] { 10, 20, 30, 40 });
        byte[] code = Chunk(image, BeamBinaryHostNames.CodeChunk);
        BinaryPrimitives.WriteInt32BigEndian(code.AsSpan(8), BeamBinaryHostNames.MatchOpcode);

        return SetChunk(image, BeamBinaryHostNames.CodeChunk, code);
    }

    internal static byte[] Start(int live, int source, int destination)
        => Instruction(
            BeamBinaryHostNames.StartOpcode,
            A(BeamBinaryHostNames.NoFailAtom),
            U(live),
            X(source),
            X(destination)
        );

    internal static byte[] Match(int live, int destination = 1)
        => Instruction(
            BeamBinaryHostNames.MatchOpcode,
            F(3),
            X(0),
            [0x17, .. U(11), .. A(BeamBinaryHostNames.EnsureAtom), .. U(8), .. U(1),
             .. A(BeamBinaryHostNames.EqualAtom), .. A(0), .. U(8), .. U(0),
             .. A(BeamBinaryHostNames.GetTailAtom), .. U(live), .. U(1), .. X(destination)]
        );

    private static byte[] Compact(int value, byte tag)
    {
        if (value < 0 || value >= 2048)
            throw new ArgumentOutOfRangeException(nameof(value));

        return value < 16 ? [checked((byte)((value << 4) | tag))]
            : [checked((byte)(((value >> 8) << 5) | 8 | tag)), (byte)(value & 255)];
    }

    private static byte[] Chunk(byte[] image, string name)
    {
        for (int cursor = 12; cursor < image.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(cursor + 4));
            if (Encoding.ASCII.GetString(image, cursor, 4) == name)
                return image.AsSpan(cursor + 8, length).ToArray();
            cursor += 8 + ((length + 3) & ~3);
        }
        throw new InvalidDataException(BeamBinaryHostNames.Failure);
    }

    private static byte[] SetChunk(byte[] image, string name, byte[] payload)
    {
        using var output = new MemoryStream();
        output.Write(image.AsSpan(0, 12));
        bool found = false;
        for (int cursor = 12; cursor < image.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(cursor + 4));
            int padded = (length + 3) & ~3;
            if (Encoding.ASCII.GetString(image, cursor, 4) == name)
            {
                if (found)
                    throw new InvalidDataException(BeamBinaryHostNames.Failure);
                WriteChunk(output, name, payload);
                found = true;
            }
            else
            {
                output.Write(image.AsSpan(cursor, 8 + padded));
            }
            cursor += 8 + padded;
        }
        if (!found)
            WriteChunk(output, name, payload);
        byte[] result = output.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(4), result.Length - 8);

        return result;
    }

    private static void WriteChunk(Stream output, string name, byte[] payload)
    {
        output.Write(Encoding.ASCII.GetBytes(name));
        Write32(output, payload.Length);
        output.Write(payload);
        output.Write(new byte[((payload.Length + 3) & ~3) - payload.Length]);
    }

    private static void Write32(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }
}
