namespace Erlang;

internal sealed partial class RuntimeProcessTaskScheduler
{
    internal RuntimeScheduledTask DequeueSelectedWork(RuntimeNormalWorkSelection selection)
        => selection.Dequeue(SelectSystem428 ? SystemPending428 : Pending);

    internal void ExecuteSelectedWork(RuntimeScheduledTask work, int laneIdentity,
        RuntimeNormalWorkSelection selection)
    {
        // Enter may fail before TryExecuteTask consumes the actual task.
        using var turn = RuntimeActorExecutionTurn.Enter(Owner, work.Epoch, laneIdentity);
        try
        {
            _ = TryExecuteTask(work.Task);
            // True and false both mean this scheduler must not replay it.
            // Ordinary actor-body exceptions are stored in Task by .NET here.
            selection.Consumed();
        }
        catch
        {
            // Never replay a body that actually started, faulted or completed.
            // Retain a task that is demonstrably still unexecuted.
            if (work.Task.Status is not TaskStatus.Created and not TaskStatus.WaitingToRun)
                selection.Consumed();
            throw;
        }
    }
}
