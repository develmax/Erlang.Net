namespace Erlang;

internal sealed partial class RuntimeCountersOwner421
{
    private int retirementWorkers596;
    private Exception? retirementFailure596;

    internal void EnqueueRetirement596(CountersArray421 array)
    {
        lock (gate)
        {
            if (draining596 || drained)
                return;
            retirementWorkers596 = checked(retirementWorkers596 + 1);
        }
        try
        {
            if (ExecutionContext.IsFlowSuppressed())
                _ = Task.Run(() => RetireQueued596(array));
            else
            {
                using (ExecutionContext.SuppressFlow())
                    _ = Task.Run(() => RetireQueued596(array));
            }
        }
        catch (Exception failure)
        {
            lock (gate)
            {
                retirementFailure596 ??= failure;
                retirementWorkers596--;
                Monitor.PulseAll(gate);
            }
            // The allocation stays in this owner. Do not interrupt heap
            // retirement: final Close reclaims it and reports this failure.
        }
    }

    private void RetireQueued596(CountersArray421 array)
    {
        try
        {
            Retire(array);
        }
        catch (Exception failure)
        {
            lock (gate)
                retirementFailure596 ??= failure;
        }
        finally
        {
            lock (gate)
            {
                retirementWorkers596--;
                Monitor.PulseAll(gate);
            }
        }
    }
}
