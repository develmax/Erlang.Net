namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
    private int asyncAccountingLeases1170;
    private readonly TaskCompletionSource asyncAccountingDrained1170 = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal RuntimeAsyncAccountingFrontendLease1170? TryAcquireAsyncAccounting1170()
    {
        var lease = new RuntimeAsyncAccountingFrontendLease1170(this);
        lock (gate)
        {
            RequireAsyncAccountingNotFailed1170();
            if (!ready || closing) return null;
            asyncAccountingLeases1170 = checked(asyncAccountingLeases1170 + 1);
            lease.Admitted = true;

            return lease;
        }
    }

    internal bool IsReadyAsyncAccounting1170()
    {
        lock (gate)
        {
            RequireAsyncAccountingNotFailed1170();

            return ready && !closing;
        }
    }

    private void RequireAsyncAccountingNotFailed1170()
    {
        if (fatal is { } failure) throw new IOException(DriverParentWireNames422.Closed, failure);
    }

    internal void ReleaseAsyncAccounting1170(RuntimeAsyncAccountingFrontendLease1170 lease,
        ref RuntimeDriverFrontend422? owner)
    {
        lock (gate)
        {
            if (!ReferenceEquals(owner, this)) return;
            if (lease.Admitted)
            {
                lease.Admitted = false;
                asyncAccountingLeases1170--;
            }
            Volatile.Write(ref owner, null);
            if (closing && asyncAccountingLeases1170 == 0) asyncAccountingDrained1170.TrySetResult();
        }
    }

    private void SealAsyncAccounting1170()
    {
        // Existing shared R is held by BeginClose/RecordFailure.
        if (asyncAccountingLeases1170 == 0) asyncAccountingDrained1170.TrySetResult();
    }

    private async Task WaitAsyncAccounting1170()
    {
        Task completed = await Task.WhenAny(asyncAccountingDrained1170.Task, child.PhysicalExit).ConfigureAwait(false);
        if (!ReferenceEquals(completed, asyncAccountingDrained1170.Task))
            throw new IOException(DriverParentWireNames422.Closed);
        await completed.ConfigureAwait(false);
    }

    internal Task<byte[]> RequestAsyncAccounting1170(ErlDriverHostOperation420 operation, byte[] data)
        => transport.AsyncAccounting1170(operation, data);

    internal Task PhysicalAsyncAccountingExit1170 => child.PhysicalExit;

    internal string AsyncAccountingSession1170 => transport.Session438;
    internal int AsyncAccountingExpectedCount1170 => configuration.SystemInfo.AsyncThreads;
    internal int AsyncAccountingMaximumFrame1170 => configuration.Launch.MaximumFrameBytes;

    internal async Task AbortAsyncAccounting1170(Exception primary)
    {
        RecordFailure(primary);
        Exception? abort = null, retirementFailure = null;
        try
        {
            await child.Peer.AbortAsync(primary).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            abort = failure;
        }
        // Do not wait for our own frontend lease before forcing unknown native
        // outcomes through the existing actually-owned child exit path.
        try
        {
            await child.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            retirementFailure = failure;
        }
        await child.PhysicalExit.ConfigureAwait(false);
        if (abort is not null && retirementFailure is not null) throw new AggregateException(abort, retirementFailure);
        if (abort is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(abort).Throw();
        if (retirementFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(retirementFailure).Throw();
    }
}
