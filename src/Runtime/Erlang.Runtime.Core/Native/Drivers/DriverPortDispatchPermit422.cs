namespace Erlang;

internal sealed class DriverPortDispatchPermit422 : IDisposable
{
    [ThreadStatic]
    private static DriverPortDispatchPermit422? current;
    private readonly DriverPortDispatchPermit422? previous;
    private readonly int thread;
    private readonly ProcessRuntime runtime;
    private bool disposed;

    private DriverPortDispatchPermit422(ProcessRuntime runtime)
    {
        this.runtime = runtime;
        previous = current;
        thread = Environment.CurrentManagedThreadId;
        current = this;
    }

    internal static DriverPortDispatchPermit422 Enter(ProcessRuntime runtime, int lane)
    {
        if (lane < 1 || RuntimeActorExecutionTurn.IsRuntime(runtime))
            throw new InvalidOperationException(DriverPortDispatchNames422.ActorLeak);

        return new(runtime);
    }

    internal static void Require(ProcessRuntime runtime)
    {
        if (current is null || !ReferenceEquals(current.runtime, runtime) || current.disposed)
            throw new InvalidOperationException(DriverPortDispatchNames422.NoOnlineLane);
    }

    public void Dispose()
    {
        if (disposed) return;
        if (Environment.CurrentManagedThreadId != thread || !ReferenceEquals(current, this))
            throw new InvalidOperationException(DriverPortDispatchNames422.NoOnlineLane);
        current = previous;
        disposed = true;
    }
}
