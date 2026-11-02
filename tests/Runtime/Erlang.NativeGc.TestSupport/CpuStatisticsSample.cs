using System.Numerics;

namespace Erlang.NifNativeBackend;

internal readonly record struct CpuStatisticsSample(BigInteger Total, BigInteger Delta);
