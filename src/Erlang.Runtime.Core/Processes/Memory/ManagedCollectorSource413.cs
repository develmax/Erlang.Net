using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Erlang;

internal static class ManagedCollectorSource413
{
    // Ordered chunk IDs, lengths and payloads bind the complete loaded official image.
    // Nonsemantic IFF padding bytes are omitted on both sides; no mutable metadata trust.
    internal static bool Matches(PreparedBeamImage image, ManagedCollectorKind413 kind, ProcessRuntime runtime)
    {
        string module = kind == ManagedCollectorKind413.Literal
            ? ManagedCollectorNames413.LiteralModule : ManagedCollectorNames413.TraceModule;
        var required = ManagedCollectorNames413.Signatures.Where(signature => signature.Module == module).ToArray();
        if (image.ExecutionIdentity.NativePrimitives.Count != required.Length) return false;
        foreach (var signature in required)
            if (!image.ExecutionIdentity.NativePrimitives.TryGetValue((signature.Function, signature.Arity), out var body)
                || body.Target is not ManagedCollectorBinding413 binding
                || !ReferenceEquals(binding.Owner, runtime) || !ReferenceEquals(binding.Body, body)
                || binding.Module != signature.Module || binding.Function != signature.Function || binding.Arity != signature.Arity)
                return false;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var chunk in image.Code.Structure.Chunks)
        {
            hash.AppendData(Encoding.ASCII.GetBytes(chunk.Id));
            BinaryPrimitives.WriteInt32BigEndian(length, chunk.Payload.Length);
            hash.AppendData(length);
            hash.AppendData(chunk.Payload.Span);
        }

        return Convert.ToHexString(hash.GetHashAndReset()) == (kind == ManagedCollectorKind413.Literal
            ? ManagedCollectorNames413.LiteralImage : ManagedCollectorNames413.TraceImage);
    }
}
