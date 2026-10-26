using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Erlang;

internal static class DirtySystemHandlerSource428
{
    internal static bool Matches(PreparedBeamImage image, ModuleRegistry registry)
    {
        if (!ReferenceEquals(image.Owner, registry) || image.Name != SystemTaskNames428.HandlerModule
            || image.NativePrimitiveExports.Count != 0) return false;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var chunk in image.Code.Structure.Chunks)
        {
            hash.AppendData(Encoding.ASCII.GetBytes(chunk.Id));
            BinaryPrimitives.WriteInt32BigEndian(length, chunk.Payload.Length);
            hash.AppendData(length);
            hash.AppendData(chunk.Payload.Span);
        }

        return Convert.ToHexString(hash.GetHashAndReset()) == SystemTaskNames428.HandlerImage;
    }
}
