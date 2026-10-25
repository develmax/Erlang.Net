namespace Erlang;

internal sealed class RuntimeActorControlLease428 : IDisposable
{
    private RuntimeActorDispatcher? dispatcher;
    private RuntimeProcessTaskScheduler? scheduler;
    internal RuntimeProcessTaskScheduler Scheduler => scheduler ?? throw new InvalidOperationException(RuntimeActorControlNames428.Missing);

    internal RuntimeActorControlLease428(RuntimeActorDispatcher dispatcher, RuntimeProcessTaskScheduler scheduler)
    {
        this.dispatcher = dispatcher;
        this.scheduler = scheduler;
    }

    public void Dispose()
    {
        var current = Interlocked.Exchange(ref dispatcher, null);
        if (current is null) return;
        try
        {
            current.ReleaseControl428(this);
        }
        finally
        {
            scheduler = null;
        }
    }
}
