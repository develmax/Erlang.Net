using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.LibrarySelectionPolicyControlNames;

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyControlBytes
{
    // Modeled admission controls only: these do not claim original OTP source equivalence.
    internal static byte[] Identity(string module, string dependency, string function)
        => Rename(
            Image([.. Start(
                1,
                2,
                2,
                1
            ), 19], importFunction: function),
            module,
            Run,
            dependency,
            function
        );

    internal static byte[] NativeStub(string module, string function)
        => Rename(
            Image([.. Start(
                1,
                2,
                2,
                1
            ), 64, A(7), X(0), 78, U(1), U(0)], importFunction: NativeTrap),
            module,
            function,
            RuntimeModule,
            NativeTrap
        );

    private static byte[] Rename(
        byte[] image,
        string module,
        string export,
        string importModule,
        string importFunction
    )
    {
        // Same fixed atom positions as BeamOpcodeEdgeBytes.Image.
        string[] atoms = [module, export, BeamOpcodeEdgeNames.Middle, BeamOpcodeEdgeNames.Leaf,
            importModule, importFunction, NativeReason, BeamOpcodeEdgeNames.Other];
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

        return BeamMetadataModeledBytes.WithChunk(image, AtomsChunk, payload.ToArray());
    }
}
