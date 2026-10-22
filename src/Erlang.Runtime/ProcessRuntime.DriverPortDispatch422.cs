namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal DriverDispatcherLease422 AcquireDriverDispatcher422() => actorDispatcher.AcquireDriverOwner422();

    internal Task DispatchDriverPort422(DriverDispatcherLease422 owner, Func<Task> callback)
        => actorDispatcher.DispatchDriverPort422(this, owner, callback);
}
