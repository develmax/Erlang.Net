namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal ValueTask<RuntimeNormalBarrierRequest696> AcquireNormalBarrier696(ProcessContext requester)
        => actorDispatcher.AcquireNormalBarrier696(requester);
}
