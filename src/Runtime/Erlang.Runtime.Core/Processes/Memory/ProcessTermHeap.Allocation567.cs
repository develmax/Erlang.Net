namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal ProcessTermAllocationSnapshot567 AllocationSnapshot567
    {
        get
        {
            lock (Gate)
            {
                long nursery = 0, survivor = 0, old = 0;
                foreach (var entry in blocks)
                {
                    long words = entry.Value.AllocationWords526;
                    if (oldBlocks508.Contains(entry.Key))
                        old = checked(old + words);
                    else if (entry.Key <= highWaterIdentity508)
                        survivor = checked(survivor + words);
                    else
                        nursery = checked(nursery + words);
                }

                return new(
                    nursery,
                    survivor,
                    old,
                    reclaimedAllocationWords567
                );
            }
        }
    }
}
