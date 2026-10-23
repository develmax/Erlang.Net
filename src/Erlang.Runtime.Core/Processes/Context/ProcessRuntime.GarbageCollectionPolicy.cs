namespace Erlang;

public sealed partial class ProcessRuntime
{
    private int defaultFullSweepAfter = ProcessFullSweepNames.DefaultThreshold;

    internal ushort DefaultFullSweepAfter => (ushort)Volatile.Read(ref defaultFullSweepAfter);

    internal ushort ExchangeDefaultFullSweepAfter(ushort value)
        => (ushort)Interlocked.Exchange(ref defaultFullSweepAfter, value);
}
