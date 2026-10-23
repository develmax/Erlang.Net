namespace Erlang;

// Prepared before start/publication. Native stop only signals an existing
// continuation; it never allocates or starts a detached cleanup task.
internal sealed class ErlDriverPortRetirement420
{
    private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task Completion { get; }

    internal ErlDriverPortRetirement420(ErlDriverPort420 port, Func<ErlDriverPort420, Task> retire)
    {
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            Completion = Run(port, retire);
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal void Stop() => stopped.TrySetResult();

    private async Task Run(ErlDriverPort420 port, Func<ErlDriverPort420, Task> retire)
    {
        await stopped.Task.ConfigureAwait(false);
        await retire(port).ConfigureAwait(false);
    }
}
