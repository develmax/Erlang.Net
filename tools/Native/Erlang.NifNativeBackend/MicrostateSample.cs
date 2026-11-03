using System.Numerics;

namespace Erlang.NifNativeBackend;

internal sealed record MicrostateSample(string Type, int Identity, IReadOnlyDictionary<string, BigInteger> Counters);
