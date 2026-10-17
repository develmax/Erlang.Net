namespace Erlang;

public sealed partial class ProcessRuntime
{
    private int backtraceDepth = ErlangStackLimits.DefaultBacktraceDepth;

    internal int ExchangeBacktraceDepth(int depth) => Interlocked.Exchange(ref backtraceDepth, depth);
}
