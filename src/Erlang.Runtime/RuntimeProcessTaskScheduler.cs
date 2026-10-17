namespace Erlang;

internal sealed class RuntimeProcessTaskScheduler(RuntimeActorDispatcher dispatcher, ProcessContext owner, RuntimeActorRole role = RuntimeActorRole.Guest) : TaskScheduler
{
    internal ProcessContext Owner { get; } = owner;
    internal RuntimeActorRole Role { get; } = role;
    internal RuntimeSchedulerLane Lane { get; set; } = null!;
    internal CancellationTokenRegistration CancellationRegistration { get; set; }

    internal void NotifyCancellation() => dispatcher.OwnerCanceled(this);

    internal Queue<RuntimeScheduledTask> Pending { get; } = new();
    internal LinkedListNode<RuntimeProcessTaskScheduler>? ReadyNode { get; set; }
    internal bool Running { get; set; }
    internal int SkipCount { get; set; }
    internal long NextEpoch { get; set; }
    internal long LastExecutedEpoch { get; set; }
    public override int MaximumConcurrencyLevel => 1;

    protected override void QueueTask(Task task) => dispatcher.Enqueue(this, task);

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
        !taskWasPreviouslyQueued && RuntimeActorExecutionTurn.IsOwnedBy(Owner) &&
        System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack() && TryExecuteTask(task);

    protected override IEnumerable<Task>? GetScheduledTasks() => dispatcher.Snapshot(this);

    internal void Execute(RuntimeScheduledTask work, int laneIdentity)
    {
        using var turn = RuntimeActorExecutionTurn.Enter(Owner, work.Epoch, laneIdentity);
        _ = TryExecuteTask(work.Task);
    }

    internal void Continue(Action body, ProcessExecutionAdmission admission)
    {
        var request = new RuntimeActorContinuation(body, admission);
        _ = Task.Factory.StartNew(
            static value => ((RuntimeActorContinuation)value!).Body(),
            request,
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            this
        );
    }

    internal Task<Term> Invoke(Func<ValueTask<Term>> body) => Task.Factory.StartNew(
        () => body().AsTask(),
        CancellationToken.None,
        TaskCreationOptions.DenyChildAttach,
        this
    ).Unwrap();
}
