using System.Numerics;

namespace Erlang;

internal sealed record BeamBinaryCommand(
    BeamBinaryCommandKind Kind,
    int Live,
    int Bits,
    int Unit,
    BigInteger Expected,
    BeamOperand? Destination,
    int Flags = 0
);
