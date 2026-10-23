using System.Numerics;

namespace Erlang;

internal readonly record struct RuntimeMemoryPayload(BigInteger Processes, BigInteger ProcessesUsed, BigInteger Native);
