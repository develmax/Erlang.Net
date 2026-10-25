namespace Erlang;

internal static class RuntimeSchedulerStatistics
{
    internal static bool IsQueue(string key) => key is RuntimeStatisticsNames.RunQueue or
        RuntimeStatisticsNames.RunQueueLengths or RuntimeStatisticsNames.RunQueueLengthsAll or
        RuntimeStatisticsNames.TotalRunQueueLengths or RuntimeStatisticsNames.TotalRunQueueLengthsAll or
        RuntimeStatisticsNames.ActiveTasks or RuntimeStatisticsNames.ActiveTasksAll or
        RuntimeStatisticsNames.TotalActiveTasks or RuntimeStatisticsNames.TotalActiveTasksAll;

    internal static bool IncludesRunning(string key) => key is RuntimeStatisticsNames.ActiveTasks or
        RuntimeStatisticsNames.ActiveTasksAll or RuntimeStatisticsNames.TotalActiveTasks or RuntimeStatisticsNames.TotalActiveTasksAll;

    internal static Term Format(string key, long[] normalQueues)
    {
        if (key is RuntimeStatisticsNames.RunQueue or RuntimeStatisticsNames.TotalRunQueueLengths or
            RuntimeStatisticsNames.TotalRunQueueLengthsAll or RuntimeStatisticsNames.TotalActiveTasks or RuntimeStatisticsNames.TotalActiveTasksAll)
            return Term.I(normalQueues.Sum());
        var result = normalQueues.Select(value => Term.I(value)).ToList();
        // This backend has no dirty execution queues. Their aggregate entries are empty.
        result.Add(Term.I(0));
        if (key is RuntimeStatisticsNames.RunQueueLengthsAll or RuntimeStatisticsNames.ActiveTasksAll)
            result.Add(Term.I(0));

        return Cons.From(result);
    }
}
