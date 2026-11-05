namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Task<RuntimeAsyncAccountingCapture1170?> CaptureAsyncAccounting1170Async(ProcessContext caller, ulong epoch)
    {
        RuntimeDriverFrontend422? frontend;
        RuntimeAsyncAccountingFrontendLease1170? lease;
        lock (gate)
        {
            RequireEtsFoundationActor(caller);
            caller.Cancellation.ThrowIfCancellationRequested();
            if (epoch == 0) throw new ArgumentOutOfRangeException(nameof(epoch));
            frontend = driverFrontendClosing422 ? null : driverFrontend422;
            lease = frontend?.TryAcquireAsyncAccounting1170();
        }
        // Statistics never starts/awaits an installing backend or invents rows.
        if (frontend is null || lease is null) return Task.FromResult<RuntimeAsyncAccountingCapture1170?>(null);

        return CreateAsyncAccounting1170(
            frontend,
            lease,
            epoch,
            caller.Cancellation
        );
    }

    private static Task<RuntimeAsyncAccountingCapture1170?> CreateAsyncAccounting1170(
        RuntimeDriverFrontend422 frontend,
        RuntimeAsyncAccountingFrontendLease1170 lease,
        ulong epoch,
        CancellationToken cancellation
    )
    {
        RuntimeAsyncAccountingCapture1170? captured = null;
        TaskCompletionSource<bool>? start = null;
        try
        {
            start = new(TaskCreationOptions.RunContinuationsAsynchronously);
            captured = new(frontend, lease, epoch);
            Task<RuntimeAsyncAccountingCapture1170?> producer = CreateAsyncAccountingCore1170(captured, start.Task, cancellation);
            if (producer.IsFaulted) producer.GetAwaiter().GetResult();
            start.TrySetResult(true);

            return producer;
        }
        catch (Exception primary)
        {
            start?.TrySetResult(false);

            return FailedAsyncAccountingPreparation1170(captured, lease, primary);
        }
    }

    private static async Task<RuntimeAsyncAccountingCapture1170?> CreateAsyncAccountingCore1170(
        RuntimeAsyncAccountingCapture1170 captured, Task<bool> start, CancellationToken cancellation)
    {
        if (!await start.ConfigureAwait(false)) return null;
        try
        {
            await captured.Initialize1170Async(cancellation).ConfigureAwait(false);

            return captured;
        }
        catch (Exception primary)
        {
            try
            {
                await captured.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private static async Task<RuntimeAsyncAccountingCapture1170?> FailedAsyncAccountingPreparation1170(
        RuntimeAsyncAccountingCapture1170? captured, RuntimeAsyncAccountingFrontendLease1170 lease, Exception primary)
    {
        try
        {
            if (captured is not null) await captured.DisposeAsync().ConfigureAwait(false);
            else lease.Dispose();
        }
        catch (Exception cleanup)
        {
            throw new AggregateException(primary, cleanup);
        }
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();

        return null;
    }

    internal bool IsAsyncAccountingCaptureCurrent1170(RuntimeAsyncAccountingCapture1170? capture)
    {
        lock (gate)
        {
            var frontend = driverFrontendClosing422 ? null : driverFrontend422;
            bool ready = frontend?.IsReadyAsyncAccounting1170() == true;

            return capture is null ? !ready
                : ready && ReferenceEquals(capture.FrontendIdentity1170, frontend) && capture.IsOpen1170;
        }
    }
}
