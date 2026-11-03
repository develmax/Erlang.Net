namespace Erlang;

public sealed partial class ProcessContext
{
    // Restore a suspended private execution through its real scheduler without
    // charging another reduction or manufacturing an actor execution turn.
    internal ProcessExecutionAwaitable ResumeExecution()
    {
        Runtime.ThrowIfHalted();
        Cancellation.ThrowIfCancellationRequested();

        return new(ValueTask.CompletedTask, ActorTaskScheduler, ProcessExecutionAdmission.Continuation);
    }
}
