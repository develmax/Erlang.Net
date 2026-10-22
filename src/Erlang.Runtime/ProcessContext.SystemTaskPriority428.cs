namespace Erlang;

public sealed partial class ProcessContext
{
    internal int RemainingSystemTaskReductions428 => Math.Max(1, ProcessSchedulingLimits.CooperativeSlice - reductions);
    private int systemTaskPriority428 = -1;
    internal int SystemTaskPriority428 => Volatile.Read(ref systemTaskPriority428);
    internal ProcessSchedulingPriority DispatchPriority428
        => ActorTaskScheduler?.DispatchPriority428 ?? SchedulingPriorityLevel;

    internal void SetSystemTaskPriority428(int priority) => Volatile.Write(ref systemTaskPriority428, priority);
}
