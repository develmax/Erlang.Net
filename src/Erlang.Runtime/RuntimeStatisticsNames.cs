namespace Erlang;

public static class RuntimeStatisticsNames
{
    public const string Function = "statistics";
    public const string WallClock = "wall_clock";
    public const string Runtime = "runtime";
    public const string NotSupported = "notsup";
    public const string ErrorModule = "module";
    public const string ErrorFormatter = "erl_erts_errors";
    public const string ActiveTasks = "active_tasks";
    public const string ActiveTasksAll = "active_tasks_all";
    public const string ContextSwitches = "context_switches";
    public const string ExactReductions = "exact_reductions";
    public const string GarbageCollection = "garbage_collection";
    public const string Io = "io";
    public const string MicrostateAccounting = "microstate_accounting";
    public const string Reductions = "reductions";
    public const string RunQueue = "run_queue";
    public const string RunQueueLengths = "run_queue_lengths";
    public const string RunQueueLengthsAll = "run_queue_lengths_all";
    public const string TotalActiveTasks = "total_active_tasks";
    public const string TotalActiveTasksAll = "total_active_tasks_all";
    public const string TotalRunQueueLengths = "total_run_queue_lengths";
    public const string TotalRunQueueLengthsAll = "total_run_queue_lengths_all";
    public const string SchedulerWallTime = "scheduler_wall_time";
    public const string SchedulerWallTimeAll = "scheduler_wall_time_all";
    public const string RunQueues = "run_queues";

    public static bool IsPhysical(string key) => key is ActiveTasks or ActiveTasksAll or ContextSwitches or
        GarbageCollection or Io or MicrostateAccounting or RunQueue or RunQueueLengths or RunQueueLengthsAll or
        TotalActiveTasks or TotalActiveTasksAll or TotalRunQueueLengths or TotalRunQueueLengthsAll or SchedulerWallTime or SchedulerWallTimeAll or RunQueues;
}
