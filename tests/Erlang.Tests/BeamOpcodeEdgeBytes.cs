using System.Buffers.Binary;
using System.Text;

namespace Erlang.Tests;

internal static class BeamOpcodeEdgeBytes
{
    // These are small, synthetic generic BEAM operands, not original OTP images.
    // All numbers below are wire opcodes/tags or local labels, not term semantics.
    internal static byte U(int value) => checked((byte)(value << 4));

    internal static byte I(int value) => checked((byte)((value << 4) | 1));

    internal static byte A(int value) => checked((byte)((value << 4) | 2));

    internal static byte X(int value) => checked((byte)((value << 4) | 3));

    internal static byte Y(int value) => checked((byte)((value << 4) | 4));

    internal static byte F(int value) => checked((byte)((value << 4) | 5));

    internal static byte[] Start(
        int info,
        int entry,
        int atom,
        int arity
    )
        => [1, U(info), 2, A(1), A(atom), U(arity), 1, U(entry)];

    internal static byte[] Image(
        byte[] instructions,
        int labels = 5,
        int functions = 1,
        int arity = 1,
        string importFunction = BeamOpcodeEdgeNames.Length,
        int importArity = 1
    )
    {
        var assembly = typeof(BeamOpcodeEdgeBytes).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamOpcodeEdgeNames.Resource, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(name)!;
        using var template = new MemoryStream();
        input.CopyTo(template);
        byte[] result = template.ToArray();
        foreach (string chunk in BeamOpcodeEdgeNames.RemovedChunks)
            result = BeamMetadataModeledBytes.WithChunk(result, chunk, null);
        string[] atoms = [BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run, BeamOpcodeEdgeNames.Middle,
            BeamOpcodeEdgeNames.Leaf, BeamOpcodeEdgeNames.Erlang, importFunction,
            BeamOpcodeEdgeNames.Marker, BeamOpcodeEdgeNames.Other];
        using var atomBytes = new MemoryStream();
        Write32(atomBytes, atoms.Length);
        foreach (string atom in atoms)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(atom);
            atomBytes.WriteByte(checked((byte)bytes.Length));
            atomBytes.Write(bytes);
        }
        using var code = new MemoryStream();
        foreach (int value in new[] { 16, 0, 169, labels, functions })
            Write32(code, value);
        code.Write(instructions);
        code.WriteByte(3);
        using var imports = new MemoryStream();
        foreach (int value in new[] { 1, 5, 6, importArity })
            Write32(imports, value);
        using var exports = new MemoryStream();
        foreach (int value in new[] { 1, 2, arity, 2 })
            Write32(exports, value);
        result = BeamMetadataModeledBytes.WithChunk(result, BeamOpcodeEdgeNames.Atoms, atomBytes.ToArray());
        result = BeamMetadataModeledBytes.WithChunk(result, BeamOpcodeEdgeNames.Imports, imports.ToArray());
        result = BeamMetadataModeledBytes.WithChunk(result, BeamOpcodeEdgeNames.Exports, exports.ToArray());

        return BeamMetadataModeledBytes.WithChunk(result, BeamOpcodeEdgeNames.Code, code.ToArray());
    }

    private static void Write32(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }
}
