namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal Term SampleQueues504(string key, DirtyNativeOwner419? dirty)
    {
        bool includeRunning = RuntimeSchedulerStatistics.IncludesRunning(key);
        lock (gate)
        {
            // Hold the normal queue lock while taking the dirty queue lock so
            // run_queue observes a single boundary across every owned queue.
            long[] normal = QueueLengths(includeRunning);
            var counts = dirty?.QueueLengths504(includeRunning) ?? default;

            return RuntimeSchedulerStatistics.Format(
                key,
                normal,
                counts.Cpu,
                counts.Io
            );
        }
    }
}
