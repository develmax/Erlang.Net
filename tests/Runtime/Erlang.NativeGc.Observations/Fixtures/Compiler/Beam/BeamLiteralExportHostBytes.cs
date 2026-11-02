using System.Buffers.Binary;
using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLiteralExportHostBytes
{
    internal static byte[] Export(Term module, Term function, Term arity)
        => [BeamLiteralExportHostNames.Version, BeamLiteralExportHostNames.ExportTag,
            .. ExternalTermFormat.Encode(module).AsSpan(1).ToArray(),
            .. ExternalTermFormat.Encode(function).AsSpan(1).ToArray(),
            .. ExternalTermFormat.Encode(arity).AsSpan(1).ToArray()];

    internal static byte[] Image(byte[] literal)
    {
        byte[] image = BeamOpcodeEdgeBytes.Image([.. Start(
            1,
            2,
            2,
            0
        ), 64,
            BeamLiteralExportHostNames.LiteralOperand, U(0), X(0), 19], labels: 3, arity: 0);
        using var table = new MemoryStream();
        Write32(table, 0);
        Write32(table, 1);
        Write32(table, literal.Length);
        table.Write(literal);

        using var complete = new MemoryStream();
        complete.Write(image);
        complete.Write(System.Text.Encoding.ASCII.GetBytes(BeamLiteralExportHostNames.LiteralChunk));
        Write32(complete, checked((int)table.Length));
        complete.Write(table.ToArray());
        complete.Write(new byte[(4 - table.Length % 4) % 4]);
        byte[] result = complete.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(4), result.Length - 8);

        return result;
    }

    internal static byte[] Local(ProcessRuntime runtime)
    {
        using var body = new MemoryStream();
        body.WriteByte(0);
        body.Write(new byte[BeamLiteralExportHostNames.UniqueBytes]);
        Write32(body, 0);
        Write32(body, 0);
        body.Write(ExternalTermFormat.Encode(Term.A(BeamLiteralExportHostNames.Module)).AsSpan(1));
        body.Write(ExternalTermFormat.Encode(Term.I(0)).AsSpan(1));
        body.Write(ExternalTermFormat.Encode(Term.I(0)).AsSpan(1));
        body.Write(ExternalTermFormat.Encode(runtime.InitializationPid).AsSpan(1));
        using var result = new MemoryStream();
        result.WriteByte(BeamLiteralExportHostNames.Version);
        result.WriteByte(BeamLiteralExportHostNames.NewFunctionTag);
        Write32(result, checked((int)body.Length + sizeof(int)));
        result.Write(body.ToArray());

        return result.ToArray();
    }

    private static void Write32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }
}
