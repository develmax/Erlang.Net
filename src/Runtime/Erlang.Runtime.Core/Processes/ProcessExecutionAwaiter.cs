using System.Runtime.CompilerServices;

namespace Erlang;

public readonly struct ProcessExecutionAwaiter : ICriticalNotifyCompletion
{
    private readonly ValueTaskAwaiter pending;
    private readonly RuntimeProcessTaskScheduler? scheduler;
    private readonly ProcessExecutionAdmission admission;

    internal ProcessExecutionAwaiter(ValueTask pending, RuntimeProcessTaskScheduler? scheduler, ProcessExecutionAdmission admission)
    {
        this.pending = pending.GetAwaiter();
        this.scheduler = scheduler;
        this.admission = admission;
    }

    private bool Forced => admission is ProcessExecutionAdmission.Reduction or ProcessExecutionAdmission.StackUnwind;
    public bool IsCompleted => !Forced && pending.IsCompleted &&
        (scheduler is null || RuntimeActorExecutionTurn.IsOwnedBy(scheduler.Owner));

    public void GetResult() => pending.GetResult();

    public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation)
    {
        var target = scheduler;
        var captured = ExecutionContext.Capture();
        var reason = admission;
        bool forced = Forced;

        void Queue()
        {
            if (target is null)
            {
                _ = Task.Run(continuation);

                return;
            }
            if (!forced && RuntimeActorExecutionTurn.IsOwnedBy(target.Owner) && RuntimeHelpers.TryEnsureSufficientExecutionStack())
                continuation();
            else
                target.Continue(continuation, reason);
        }

        void Resume()
        {
            if (captured is null)
                Queue();
            else
                ExecutionContext.Run(captured, _ => Queue(), null);
        }
        if (pending.IsCompleted)
            Resume();
        else
            pending.UnsafeOnCompleted(Resume);
    }
}
