namespace Erlang;

internal static class RuntimeSchedulerStatistics
{
    internal static bool IsQueue(string key) => key is RuntimeStatisticsNames.RunQueue or RuntimeStatisticsNames.RunQueues or
        RuntimeStatisticsNames.RunQueueLengths or RuntimeStatisticsNames.RunQueueLengthsAll or
        RuntimeStatisticsNames.TotalRunQueueLengths or RuntimeStatisticsNames.TotalRunQueueLengthsAll or
        RuntimeStatisticsNames.ActiveTasks or RuntimeStatisticsNames.ActiveTasksAll or
        RuntimeStatisticsNames.TotalActiveTasks or RuntimeStatisticsNames.TotalActiveTasksAll;

    internal static bool IncludesRunning(string key) => key is RuntimeStatisticsNames.ActiveTasks or
        RuntimeStatisticsNames.ActiveTasksAll or RuntimeStatisticsNames.TotalActiveTasks or RuntimeStatisticsNames.TotalActiveTasksAll;

    internal static bool IncludesDirtyIo(string key) => key is RuntimeStatisticsNames.RunQueues or
        RuntimeStatisticsNames.RunQueueLengthsAll or RuntimeStatisticsNames.TotalRunQueueLengthsAll or
        RuntimeStatisticsNames.ActiveTasksAll or RuntimeStatisticsNames.TotalActiveTasksAll;

    internal static Term Format(
        string key,
        long[] normalQueues,
        long dirtyCpu = 0,
        long dirtyIo = 0
    )
    {
        long[] queues = IncludesDirtyIo(key)
            ? [.. normalQueues, dirtyCpu, dirtyIo]
            : [.. normalQueues, dirtyCpu];

        if (key is RuntimeStatisticsNames.RunQueue or RuntimeStatisticsNames.TotalRunQueueLengths or
            RuntimeStatisticsNames.TotalRunQueueLengthsAll or RuntimeStatisticsNames.TotalActiveTasks or RuntimeStatisticsNames.TotalActiveTasksAll)
            return Term.I(queues.Sum());

        Term[] result = queues.Select(value => (Term)Term.I(value)).ToArray();
        if (key == RuntimeStatisticsNames.RunQueues)
            return new TupleTerm(result);

        return Cons.From(result);
    }
}
