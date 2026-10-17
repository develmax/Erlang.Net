namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal long[] QueueLengths(bool includeRunning)
    {
        lock (gate)
        {
            var result = new long[lanes.Length];
            foreach (var lane in lanes)
            {
                long count = 0;
                foreach (var queue in new[] { lane.Maximum, lane.High, lane.Ordinary, lane.Cleanup })
                    foreach (var scheduler in queue)
                        if (scheduler.Role == RuntimeActorRole.Guest)
                            count++;
                if (includeRunning && lane.Running is { Role: RuntimeActorRole.Guest })
                    count++;
                result[lane.Identity - ProcessSchedulerNames.First] = count;
            }

            return result;
        }
    }
}
