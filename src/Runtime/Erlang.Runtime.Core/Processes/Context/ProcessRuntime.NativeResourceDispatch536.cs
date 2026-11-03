namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal NativeResourceDispatcherLease536 AcquireNativeResourceDispatcher536()
        => actorDispatcher.AcquireNativeResourceOwner536(this);
}
