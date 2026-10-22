using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.NativeStubShape400Names;

namespace Erlang.Tests;

internal static class NativeStubShape400Bytes
{
    internal static byte[] Image(
        string module,
        string function,
        string reason,
        int arity = 1,
        bool extraInstruction = false,
        string import = NifError,
        int destination = 0,
        int callArity = 1,
        int importArity = 1,
        string importModule = ErlangModule
    )
    {
        byte[] prefix = BeamOpcodeEdgeBytes.Start(
            1,
            2,
            2,
            arity
        );
        byte[] extra = extraInstruction ? BeamBinaryHostBytes.Instruction(Move, BeamBinaryHostBytes.X(0), BeamBinaryHostBytes.X(1)) : [];
        byte[] image = BeamOpcodeEdgeBytes.Image(
            BeamBinaryHostBytes.Join(
                prefix,
                extra,
                BeamBinaryHostBytes.Instruction(Move, BeamBinaryHostBytes.A(7), BeamBinaryHostBytes.X(destination)),
                BeamBinaryHostBytes.Instruction(ExternalTailCall, BeamBinaryHostBytes.U(callArity), BeamBinaryHostBytes.U(0))
            ),
            labels: 3,
            arity: arity,
            importFunction: import,
            importArity: importArity
        );
        string[] atoms = [module, function, Middle, Leaf, importModule, import, reason, Other];
        using var payload = new MemoryStream();
        Span<byte> count = stackalloc byte[4];
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
