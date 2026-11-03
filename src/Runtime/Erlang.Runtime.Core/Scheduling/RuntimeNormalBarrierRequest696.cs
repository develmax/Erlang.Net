namespace Erlang;

internal sealed class RuntimeNormalBarrierRequest696 : IDisposable
{
    private RuntimeActorDispatcher? dispatcher;
    private RuntimeProcessTaskScheduler? scheduler;
    internal RuntimeProcessTaskScheduler Scheduler696
        => scheduler ?? throw new ObjectDisposedException(RuntimeNormalBarrierNames696.Lease);
    internal LinkedListNode<RuntimeNormalBarrierRequest696> Node696 { get; }
    internal TaskCompletionSource Ready696 { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Granted696 { get; set; }

    internal RuntimeNormalBarrierRequest696(RuntimeActorDispatcher dispatcher, RuntimeProcessTaskScheduler scheduler)
    {
        this.dispatcher = dispatcher;
        this.scheduler = scheduler;
        Node696 = new(this);
    }

    internal void Cancel696() => Volatile.Read(ref dispatcher)?.CancelNormalBarrier696(this);

    internal void Detach696()
    {
        scheduler = null;
        Volatile.Write(ref dispatcher, null);
    }

    public void Dispose() => Volatile.Read(ref dispatcher)?.ReleaseNormalBarrier696(this);
}
