namespace Erlang;

internal sealed partial class RuntimeProcessTaskScheduler
{
    internal Queue<RuntimeScheduledTask> SystemPending428 { get; } = new();
    internal int WorkCount428 => Pending.Count + SystemPending428.Count;

    // Called under the real dispatcher gate only. User process_flag state is
    // never overwritten by a temporary system-task priority elevation.
    internal ProcessSchedulingPriority DispatchPriority428
    {
        get
        {
            int user = Pending.Count != 0 && !Owner.SuspensionRequested ? (int)Owner.SchedulingPriorityLevel : -1;
            int system = SystemPending428.Count != 0 ? Owner.SystemTaskPriority428 : -1;
            int highest = Math.Max(user, system);

            return highest < 0 ? Owner.SchedulingPriorityLevel : (ProcessSchedulingPriority)highest;
        }
    }

    private bool SelectSystem428 => SystemPending428.Count != 0 &&
        (Pending.Count == 0 || Owner.SuspensionRequested || Owner.SystemTaskPriority428 >= (int)Owner.SchedulingPriorityLevel);

    internal RuntimeScheduledTask PeekWork428() => SelectSystem428 ? SystemPending428.Peek() : Pending.Peek();

    internal RuntimeScheduledTask DequeueWork428() => SelectSystem428 ? SystemPending428.Dequeue() : Pending.Dequeue();

    internal void EnqueueWork428(RuntimeScheduledTask work)
    {
        if (work.Task.AsyncState is SystemTaskPump428) SystemPending428.Enqueue(work);
        else Pending.Enqueue(work);
    }

    internal Task<Term> InvokeSystemTask428(Func<ValueTask<Term>> body) => Task.Factory.StartNew(
        static state => ((SystemTaskPump428)state!).Body().AsTask(),
        new SystemTaskPump428(body),
        CancellationToken.None,
        TaskCreationOptions.DenyChildAttach,
        this
    ).Unwrap();
}
