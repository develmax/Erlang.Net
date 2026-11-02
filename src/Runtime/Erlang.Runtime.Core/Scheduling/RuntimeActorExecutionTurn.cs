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

    // The existing thread-owned actor turn is the only initial execution seam.
    // This does not install a turn or derive authority from an AsyncLocal value.
    internal static ProcessContext? CurrentLiteralContext801(ProcessRuntime runtime)
        => owner is not null && ReferenceEquals(owner.Runtime, runtime) ? owner : null;

    internal static bool IsRuntime(ProcessRuntime runtime) => owner is not null && ReferenceEquals(owner.Runtime, runtime);

    internal static bool IsOwnedBy(ProcessContext context) => ReferenceEquals(owner, context);

    internal static long Epoch(ProcessContext context) => IsOwnedBy(context) ? epoch : 0;

    internal static Scope Enter(ProcessContext context, long currentEpoch, int currentLane)
    {
        var previous = new Scope(
            owner,
            epoch,
            laneIdentity,
            RuntimeNumericMapComparison.EnterThread(context.Runtime)
        );
        owner = context;
        epoch = currentEpoch;
        laneIdentity = currentLane;

        return previous;
    }

    internal readonly struct Scope(
        ProcessContext? previousOwner,
        long previousEpoch,
        int previousLane,
        TermMapComparisonScope mapComparison
    ) : IDisposable
    {
        public void Dispose()
        {
            mapComparison.Dispose();
            owner = previousOwner;
            epoch = previousEpoch;
            laneIdentity = previousLane;
        }
    }
}
