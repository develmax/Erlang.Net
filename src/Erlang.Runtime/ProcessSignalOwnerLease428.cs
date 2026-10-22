namespace Erlang;

internal sealed class ProcessSignalOwnerLease428 : IDisposable
{
    private ProcessRuntime? runtime;
    private ProcessContext? target;
    internal bool DirtyHandler { get; }

    internal ProcessSignalOwnerLease428(ProcessRuntime runtime, ProcessContext target, bool dirtyHandler)
    {
        DirtyHandler = dirtyHandler;
        this.runtime = runtime;
        this.target = target;
    }

    public void Dispose()
    {
        var owner = Interlocked.Exchange(ref runtime, null);
        if (owner is null) return;
        var context = target!;
        try
        {
            owner.ReleaseSignalOwner428(context, this);
        }
        finally
        {
            target = null;
        }
    }
}
