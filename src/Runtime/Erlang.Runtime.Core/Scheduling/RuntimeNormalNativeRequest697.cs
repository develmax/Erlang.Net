namespace Erlang;

internal sealed class RuntimeNormalNativeRequest697 : IDisposable
{
    private RuntimeActorDispatcher? dispatcher;
    private ProcessContext? context;
    internal ProcessContext Context697
        => context ?? throw new ObjectDisposedException(RuntimeNormalNativeNames697.Closed);
    internal LinkedListNode<RuntimeNormalNativeRequest697> Node697 { get; }
    internal TaskCompletionSource Ready697 { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal bool Active697 { get; set; }

    internal RuntimeNormalNativeRequest697(RuntimeActorDispatcher dispatcher, ProcessContext context)
    {
        this.dispatcher = dispatcher;
        this.context = context;
        Node697 = new(this);
    }

    internal void Cancel697() => Volatile.Read(ref dispatcher)?.CancelNormalNative697(this);

    internal void Detach697()
    {
        context = null;
        Volatile.Write(ref dispatcher, null);
    }

    public void Dispose() => Volatile.Read(ref dispatcher)?.ReleaseNormalNative697(this);
}
