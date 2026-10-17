namespace Erlang;

internal static class RuntimeActorExecutionTurn
{
    [ThreadStatic]
    private static ProcessContext? owner;
    [ThreadStatic]
    private static long epoch;

    internal static bool IsOwnedBy(ProcessContext context) => ReferenceEquals(owner, context);

    internal static long Epoch(ProcessContext context) => IsOwnedBy(context) ? epoch : 0;

    internal static Scope Enter(ProcessContext context, long currentEpoch)
    {
        var previous = new Scope(owner, epoch);
        owner = context;
        epoch = currentEpoch;

        return previous;
    }

    internal readonly struct Scope(ProcessContext? previousOwner, long previousEpoch) : IDisposable
    {
        public void Dispose()
        {
            owner = previousOwner;
            epoch = previousEpoch;
        }
    }
}
