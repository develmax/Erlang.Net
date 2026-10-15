namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeActorDispatcher actorDispatcher;

    public int ActorDispatchCapacity => actorDispatcher.Capacity;
    internal Task ActorDispatchCompletion => actorDispatcher.Completion;

    private void RollbackActorStart(ProcessContext context, bool enqueued)
    {
        foreach (Pid linked in context.Links)
        {
            if (processes.TryGetValue(linked, out var peer))
            {
                peer.LinkIdentities.Remove(context.Self);
                peer.Links.Remove(context.Self);
                peer.PriorityLinks.Remove(context.Self);
            }
        }
        context.Links.Clear();
        context.LinkIdentities.Clear();
        context.PriorityLinks.Clear();
        processes.Remove(context.Self);
        processSlots.Remove(context.Self);
        context.Terminated = true;
        Term reason = Term.A(ProcessExitReasons.Shutdown);
        context.ExitReason = reason;
        context.Stop.Cancel();
        context.Done.TrySetResult(reason);
        if (!enqueued)
        {
            executingProcesses.Remove(context);
            context.Unwound.TrySetResult();
        }
    }
}
