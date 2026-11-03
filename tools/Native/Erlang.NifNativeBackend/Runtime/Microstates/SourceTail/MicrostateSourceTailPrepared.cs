using Erlang;

namespace Erlang.NifNativeBackend;

internal sealed record MicrostateSourceTailPrepared(
    ProcessContext Context,
    RuntimeActorDispatcher Dispatcher,
    RuntimeMicrostateRequest Request,
    RuntimeSchedulerAuxiliaryBatch420 Batch,
    RuntimeMicrostateSourceTail1149 Tail,
    int SourceLane,
    object SourcePhysicalThread
);
