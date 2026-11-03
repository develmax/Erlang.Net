using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.BeamInfallibleBifControlNames;

namespace Erlang.Tests;

internal static class BeamInfallibleBifControlBytes
{
    internal static byte[] Image(
        string function,
        int arity,
        bool gc = false,
        bool opaque = false,
        bool missingArgument = false
    )
    {
        byte source = opaque ? Y(1) : missingArgument ? X(arity) : X(0);
        byte[] call = arity == 1
            ? gc ? [UnaryGc, F(0), U(arity), U(0), source, Y(0)] : [Unary, F(0), U(0), source, Y(0)]
            : [Binary, F(0), U(0), source, X(1), Y(0)];
        byte[] install = [Catch, Y(1), F(3)];

        // No init_yregs here on purpose: the admitted infallible BIF initializes
        // Y0 before a real handler tag occupies Y1. No GC may occur beforehand.
        return BeamOpcodeEdgeBytes.Image(
            [
            .. Start(
                1,
                2,
                2,
                arity
            ),Allocate,U(2),U(arity),
            .. (opaque ? install:[]),.. call,.. (opaque ? Array.Empty<byte>():install),
            Move,Y(0),X(0),CatchEnd,Y(1),Deallocate,U(2),Return,
            Label,U(3),CatchEnd,Y(1),Deallocate,U(2),Return
        ],
            labels: 4,
            arity: arity,
            importFunction: function,
            importArity: arity
        );
    }

    internal static byte[] Identity(string function, int arity)
    {
        byte[] operation = arity == UnaryIdentityArity
            ? [Unary, F(0), U(BuiltinImport), X(0), X(1),
               Unary, F(0), U(BuiltinImport), X(0), X(0)]
            : [Move, X(0), X(2),
               Binary, F(0), U(BuiltinImport), X(0), X(1), X(0),
               Move, X(2), X(1)];
        byte[] image = BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                arity
            ), .. operation,
             ExternalTail, U(SameArity), U(SameImport)],
            labels: 3,
            arity: arity,
            importFunction: function,
            importArity: arity
        );
        string[] names = [BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run,
            BeamOpcodeEdgeNames.Middle, BeamOpcodeEdgeNames.Leaf,
            BeamOpcodeEdgeNames.Erlang, function,
            DebugSame420HostNames.Module, DebugSame420HostNames.Function];
        using var atoms = new MemoryStream();
        WriteIdentityWord(atoms, names.Length);
        foreach (string name in names)
        {
            byte[] value = Encoding.UTF8.GetBytes(name);
            atoms.WriteByte(checked((byte)value.Length));
            atoms.Write(value);
        }
        image = BeamMetadataModeledBytes.WithChunk(image, BeamOpcodeEdgeNames.Atoms, atoms.ToArray());
        using var imports = new MemoryStream();
        foreach (int value in new[] { IdentityImportCount, ErlangAtom, BuiltinAtom,
            arity, SameModuleAtom, SameFunctionAtom, SameArity })
            WriteIdentityWord(imports, value);

        return BeamMetadataModeledBytes.WithChunk(image, BeamOpcodeEdgeNames.Imports, imports.ToArray());
    }

    private static void WriteIdentityWord(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }

    internal static byte[] Remote(string function, int arity)
        => BeamOpcodeEdgeBytes.Image(
            [.. Start(
                1,
                2,
                2,
                arity
            ),ExternalTail,U(arity),U(0)],
            labels: 3,
            arity: arity,
            importFunction: function,
            importArity: arity
        );
}
