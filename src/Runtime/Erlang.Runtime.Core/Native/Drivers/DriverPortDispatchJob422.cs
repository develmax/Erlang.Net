namespace Erlang;

// Queue ownership is managed metadata only. Drop the delegated callback before
// physical permit completion so a retained completion Task cannot root IPC args.
internal sealed class DriverPortDispatchJob422
{
    private Func<Task>? callback;
    private readonly ProcessRuntime runtime;

    internal DriverPortDispatchJob422(ProcessRuntime runtime, Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        this.runtime = runtime;
        this.callback = callback;
    }

    internal void Execute(int lane)
    {
        Func<Task>? action = Interlocked.Exchange(ref callback, null);
        if (action is null) throw new InvalidOperationException(DriverPortDispatchNames422.MissingCallback);
        try
        {
            // RuntimeSchedulerAuxiliary420.Execute owns this real lane until
            // this method returns. Do not fire-and-forget or spoof a lane flag.
            using var permit = DriverPortDispatchPermit422.Enter(runtime, lane);
            using var microstate = RuntimeMicrostateExecution.EnterState(runtime.Statistics.Microstates, RuntimeMicrostate.Port);
            action().GetAwaiter().GetResult();
        }
        finally
        {
            action = null;
        }
    }

    internal void Abandon() => Interlocked.Exchange(ref callback, null);
}
