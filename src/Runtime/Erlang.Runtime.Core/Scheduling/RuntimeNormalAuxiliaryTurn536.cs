namespace Erlang;

// Only the actual dispatcher Pump enters after selecting a trusted resource job.
internal static class RuntimeNormalAuxiliaryTurn536
{
    [ThreadStatic] private static ProcessRuntime? currentRuntime;
    [ThreadStatic] private static int currentLane, depth;
    internal static int Lane536 => currentLane;

    internal static bool IsRuntime536(ProcessRuntime runtime) => ReferenceEquals(currentRuntime, runtime) && currentLane != 0;

    internal static Scope Enter536(ProcessRuntime? runtime, int lane)
    {
        if (runtime is null) return default;
        if (lane < 1 || RuntimeActorExecutionTurn.IsRuntime(runtime) || DirtyNativeThread419.Owner is not null)
            throw new InvalidOperationException(NativeResource536Names.InvalidLane);
        int nextDepth = checked(depth + 1);
        var scope = new Scope(
            currentRuntime,
            currentLane,
            depth,
            nextDepth,
            Environment.CurrentManagedThreadId
        );
        currentRuntime = runtime;
        currentLane = lane;
        depth = nextDepth;

        return scope;
    }

    internal readonly struct Scope(
        ProcessRuntime? previousRuntime,
        int previousLane,
        int previousDepth,
        int admittedDepth,
        int thread
    ) : IDisposable
    {
        public void Dispose()
        {
            if (admittedDepth == 0) return;
            if (thread != Environment.CurrentManagedThreadId || depth != admittedDepth)
                throw new InvalidOperationException(NativeResource536Names.InvalidLane);
            currentRuntime = previousRuntime;
            currentLane = previousLane;
            depth = previousDepth;
        }
    }
}
