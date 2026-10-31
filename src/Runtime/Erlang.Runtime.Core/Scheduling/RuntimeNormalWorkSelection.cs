namespace Erlang;

// Allocated before selection mutates queues; owned by one real Pump iteration.
internal sealed class RuntimeNormalWorkSelection(RuntimeSchedulerLane lane)
{
    internal RuntimeSchedulerLane Lane { get; } = lane;
    internal RuntimeProcessTaskScheduler? Scheduler { get; private set; }
    internal RuntimeSchedulerAuxiliary420? Auxiliary { get; private set; }
    internal LinkedList<RuntimeProcessTaskScheduler>? OriginalReadyQueue { get; private set; }
    internal LinkedListNode<RuntimeProcessTaskScheduler>? OriginalReadyNode { get; private set; }
    internal RuntimeScheduledTask? Work { get; private set; }
    internal long PreviousExecutedEpoch { get; private set; }
    internal bool WallTimeStarted { get; set; }
    internal bool ExecutionConsumed { get; private set; }
    private Queue<RuntimeScheduledTask>? workQueue;
    private bool removed;
    private bool finalized;

    internal void Acquire(LinkedListNode<RuntimeProcessTaskScheduler> node)
    {
        Scheduler = node.Value;
        OriginalReadyNode = node;
        OriginalReadyQueue = node.List!;
        PreviousExecutedEpoch = node.Value.LastExecutedEpoch;
    }

    internal void Acquire(RuntimeSchedulerAuxiliary420 auxiliary) => Auxiliary = auxiliary;

    internal RuntimeScheduledTask Dequeue(Queue<RuntimeScheduledTask> queue)
    {
        // Preserve the actual queue and object BEFORE its head is removed.
        Work = queue.Peek();
        workQueue = queue;
        RuntimeScheduledTask value = queue.Dequeue();
        removed = true;

        return value;
    }

    internal void Consumed() => ExecutionConsumed = true;

    internal bool BeginFinalize()
    {
        if (finalized) return false;
        finalized = true;

        return true;
    }

    internal void RestoreUnexecuted()
    {
        if (!removed || ExecutionConsumed) return;
        // Restore original FIFO order without replacing the Queue object or
        // disturbing work concurrently appended while the actor was running.
        int following = workQueue!.Count;
        workQueue.Enqueue(Work!);
        for (int index = 0; index < following; index++)
            workQueue.Enqueue(workQueue.Dequeue());
        removed = false;
    }
}
