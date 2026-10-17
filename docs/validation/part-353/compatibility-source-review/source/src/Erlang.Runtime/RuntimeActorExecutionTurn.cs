namespace Erlang;

internal static class RuntimeActorExecutionTurn
{
    [ThreadStatic]
    private static ProcessContext? owner;
    [ThreadStatic]
    private static long epoch;
    [ThreadStatic]
    private static int laneIdentity;

    internal static int LaneIdentity => laneIdentity;

    internal static bool IsRuntime(ProcessRuntime runtime) => owner is not null && ReferenceEquals(owner.Runtime, runtime);

    internal static bool IsOwnedBy(ProcessContext context) => ReferenceEquals(owner, context);

    internal static long Epoch(ProcessContext context) => IsOwnedBy(context) ? epoch : 0;

    internal static Scope Enter(ProcessContext context, long currentEpoch, int currentLane)
    {
        var previous = new Scope(owner, epoch, laneIdentity);
        owner = context;
        epoch = currentEpoch;
        laneIdentity = currentLane;

        return previous;
    }

    internal readonly struct Scope(ProcessContext? previousOwner, long previousEpoch, int previousLane) : IDisposable
    {
        public void Dispose()
        {
            owner = previousOwner;
            epoch = previousEpoch;
            laneIdentity = previousLane;
        }
    }
}
