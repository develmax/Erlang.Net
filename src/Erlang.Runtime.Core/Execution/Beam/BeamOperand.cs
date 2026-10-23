using System.Numerics;

namespace Erlang;

internal sealed record BeamOperand(string Kind, BigInteger Value, int? TypeIndex = null);
