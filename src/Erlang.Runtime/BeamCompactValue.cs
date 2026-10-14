using System.Numerics;

namespace Erlang;

internal sealed record BeamCompactValue(
    int WireTag,
    BigInteger Value,
    bool FitsNativeWord,
    int EncodedPayloadBytes
);
