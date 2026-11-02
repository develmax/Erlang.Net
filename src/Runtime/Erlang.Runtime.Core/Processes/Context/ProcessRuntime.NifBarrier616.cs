namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal NifActorAdmission616<RuntimeNormalBarrierRequest696> AcquireNifBarrier616(ProcessContext requester)
        => actorDispatcher.AcquireNifBarrier616(requester);
}
