namespace Erlang;

internal sealed partial class RuntimeAtomicsOwner413
{
    private int retirementWorkers586;
    private Exception? retirementFailure586;

    internal void EnqueueRetirement586(AtomicsArray413 array)
    {
        lock (gate)
        {
            if (draining586 || drained)
                return;
            retirementWorkers586 = checked(retirementWorkers586 + 1);
        }
        try
        {
            if (ExecutionContext.IsFlowSuppressed())
                _ = Task.Run(() => RetireQueued586(array));
            else
            {
                using (ExecutionContext.SuppressFlow())
                    _ = Task.Run(() => RetireQueued586(array));
            }
        }
        catch (Exception failure)
        {
            lock (gate)
            {
                retirementFailure586 ??= failure;
                retirementWorkers586--;
                Monitor.PulseAll(gate);
            }
            // The allocation stays in this owner. Do not interrupt heap
            // retirement: final Close reclaims it and reports this failure.
        }
    }

    private void RetireQueued586(AtomicsArray413 array)
    {
        try
        {
            Retire(array);
        }
        catch (Exception failure)
        {
            lock (gate)
                retirementFailure586 ??= failure;
        }
        finally
        {
            lock (gate)
            {
                retirementWorkers586--;
                Monitor.PulseAll(gate);
            }
        }
    }
}
