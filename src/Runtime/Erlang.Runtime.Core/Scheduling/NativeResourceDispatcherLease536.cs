namespace Erlang;

internal sealed class NativeResourceDispatcherLease536(
    RuntimeActorDispatcher dispatcher,
    ProcessRuntime runtime
) : IDisposable
{
    private int released;
    internal ProcessRuntime Runtime536 { get; } = runtime;

    internal bool IsOwnedBy536(RuntimeActorDispatcher owner) => ReferenceEquals(dispatcher, owner) && Volatile.Read(ref released) == 0;

    internal bool MarkReleased536() => Interlocked.Exchange(ref released, 1) == 0;

    internal Task Dispatch536(Action body) => dispatcher.DispatchNativeResource536(this, body);

    public void Dispose() => dispatcher.ReleaseNativeResourceOwner536(this);
}
