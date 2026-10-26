using System.Numerics;

namespace Erlang;

internal sealed class ObservedPayloadEntry(BigInteger bytes, ObservedPayloadKind kind)
{
    internal BigInteger Bytes { get; } = bytes;
    internal ObservedPayloadKind Kind { get; set; } = kind;
}
