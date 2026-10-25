namespace Erlang;

internal sealed partial class RuntimeCountersOwner421
{
    private Task? shutdown;

    internal bool ShutdownCompleted => shutdown?.IsCompletedSuccessfully == true;

    internal Task BeginClose()
    {
        lock (gate)
        {
            if (shutdown is not null) return shutdown;

            // Construct and start while the owner gate prevents Close from
            // overtaking publication. A failed Start leaves admission open
            // and no unfinishable completion task installed.
            Task pending;
            if (ExecutionContext.IsFlowSuppressed())
                pending = new Task(Close, CancellationToken.None, TaskCreationOptions.DenyChildAttach);
            else
            {
                using (ExecutionContext.SuppressFlow())
                    pending = new Task(Close, CancellationToken.None, TaskCreationOptions.DenyChildAttach);
            }
            pending.Start(TaskScheduler.Default);
            shutdown = pending;

            return pending;
        }
    }
}
