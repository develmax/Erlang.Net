using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erlang;

internal static class ErlDriverPrimitiveWire420
{
    private static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    internal static byte[] Encode<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    internal static T Decode<T>(byte[]? bytes)
    {
        if (bytes is null) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return JsonSerializer.Deserialize<T>(bytes, Options)
            ?? throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }
}
