using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Erlang;

internal static class BeamChecksumReader
{
    private static readonly ConditionalWeakTable<BeamLinkedCode, BitString> checksums = new();

    internal static BitString Read(BeamLinkedCode code) => checksums.GetValue(code, Compute);

    private static BitString Compute(BeamLinkedCode code)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        foreach (string id in BeamChecksumWire.BeforeLambda)
        {
            AppendChunk(hash, code, id);
        }
        BeamChunk? lambda = code.Structure.Chunks.LastOrDefault(chunk => chunk.Id == BeamLambdaWire.Chunk);
        if (lambda is { Payload.Length: > 0 })
        {
            var cursor = new BeamCursor(lambda.Payload);
            hash.AppendData(cursor.Read(BeamChecksumWire.LambdaHeaderBytes).Span);
            ReadOnlySpan<byte> zeros = new byte[BeamChecksumWire.LambdaOldUniquenessBytes];
            while (cursor.Remaining >= BeamLambdaWire.EntryBytes)
            {
                hash.AppendData(cursor.Read(BeamChecksumWire.LambdaSignificantBytes).Span);
                cursor.Read(BeamChecksumWire.LambdaOldUniquenessBytes);
                hash.AppendData(zeros);
            }
            if (cursor.Remaining != 0)
            {
                throw new InvalidDataException(BeamLambdaDiagnostics.Table);
            }
        }
        foreach (string id in BeamChecksumWire.AfterLambda)
        {
            AppendChunk(hash, code, id);
        }

        return new BitString(hash.GetHashAndReset());
    }

    private static void AppendChunk(IncrementalHash hash, BeamLinkedCode code, string id)
    {
        BeamChunk? chunk = code.Structure.Chunks.LastOrDefault(value => value.Id == id);
        if (chunk is not null)
        {
            hash.AppendData(chunk.Payload.Span);
        }
    }
}
