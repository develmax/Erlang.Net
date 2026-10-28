namespace Erlang;

internal sealed partial class DirtyNativeOwner419
{
    internal (long Cpu, long Io) QueueLengths504(bool includeRunning)
    {
        lock (gate)
        {
            long cpuCount = 0;
            long ioCount = 0;
            foreach (var invocation in invocations)
            {
                // Cancellation and process death do not remove the queued work or
                // retire its physical worker. Count until actual dequeue/completion.
                if (invocation.Running && !includeRunning)
                    continue;

                if (invocation.Kind == DirtyNativeKind419.Cpu)
                    cpuCount++;
                else if (invocation.Kind == DirtyNativeKind419.Io)
                    ioCount++;
            }

            return (cpuCount, ioCount);
        }
    }
}
