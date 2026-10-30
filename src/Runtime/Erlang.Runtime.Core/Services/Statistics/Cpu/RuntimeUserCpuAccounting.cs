using System.Numerics;

namespace Erlang;

// This object intentionally has no reference back to the owning VM.
internal sealed class RuntimeUserCpuAccounting
{
    // Read/written only under RuntimeUserCpuOwnership's shared attribution gate.
    internal BigInteger AccumulatedTicks;
    internal Exception? Unavailable;

    internal IDisposable Enter() => RuntimeUserCpuOwnership.Enter(this);

    internal BigInteger SampleMilliseconds()
        => RuntimeUserCpuOwnership.SampleTicks(this) / TimeSpan.TicksPerMillisecond;
}
