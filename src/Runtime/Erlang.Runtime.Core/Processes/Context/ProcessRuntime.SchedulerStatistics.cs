namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term SampleSchedulerQueues(string key) => RuntimeSchedulerStatistics.Format(
        key,
        actorDispatcher.QueueLengths(RuntimeSchedulerStatistics.IncludesRunning(key))
    );
}
