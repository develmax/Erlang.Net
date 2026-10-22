using System.Buffers.Binary;
using System.Text;

namespace Erlang.Tests;

internal static class BeamLoaderDependencies410Bytes
{
    internal static byte[] Caller(string module)
        => Image(
            module,
            BeamLoaderDependencies410Names.Erlang,
            BeamOpcodeEdgeNames.Length,
            1,
            [64, BeamOpcodeEdgeBytes.I(1), BeamOpcodeEdgeBytes.X(0), 19]
        );

    internal static byte[] Target()
        => Image(
            BeamLoaderDependencies410Names.TargetModule,
            BeamLoaderDependencies410Names.DependencyModule,
            BeamLoaderDependencies410Names.Answer,
            0,
            [78, BeamOpcodeEdgeBytes.U(0), BeamOpcodeEdgeBytes.U(0)]
        );

    private static byte[] Image(
        string module,
        string dependency,
        string function,
        int arity,
        byte[] body
    )
    {
        byte[] code = [.. BeamOpcodeEdgeBytes.Start(
            1,
            2,
            2,
            0
        ), .. body];
        byte[] image = BeamOpcodeEdgeBytes.Image(
            code,
            labels: 3,
            arity: 0,
            importFunction: function,
            importArity: arity
        );
        string[] atoms = [module, BeamLoaderDependencies410Names.Run, BeamOpcodeEdgeNames.Middle,
            BeamOpcodeEdgeNames.Leaf, dependency, function, BeamOpcodeEdgeNames.Marker, BeamOpcodeEdgeNames.Other];
        using var output = new MemoryStream();
        Span<byte> count = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(count, atoms.Length);
        output.Write(count);
        foreach (string atom in atoms)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(atom);
            output.WriteByte(checked((byte)bytes.Length));
            output.Write(bytes);
        }

        return BeamMetadataModeledBytes.WithChunk(image, BeamLoaderDependencies410Names.Atoms, output.ToArray());
    }
}
