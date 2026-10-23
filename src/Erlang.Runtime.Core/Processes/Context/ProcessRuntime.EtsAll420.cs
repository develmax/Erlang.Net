namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal int EtsCreationLane420(ProcessContext context) => actorDispatcher.AssignedLane420(context);

    internal RuntimeSchedulerAuxiliaryBatch420 PrepareEtsAllLanes420(Action<int> body) => actorDispatcher.PrepareAuxiliary420(body);

    internal bool EtsAllRecipient420(ProcessContext context)
     => !disposed && !IsHaltRequested && !context.Terminated && processes.TryGetValue(context.Self, out var current) && ReferenceEquals(current, context);
}
