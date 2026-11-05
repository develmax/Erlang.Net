namespace Erlang;

// A real parent/frontend + authenticated child capture. Never a physical-thread
// registration, collector promotion, or lazily created backend.
internal sealed class RuntimeAsyncAccountingCapture1170 : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly RuntimeDriverFrontend422 frontend;
    private readonly RuntimeAsyncAccountingFrontendLease1170 frontendLease;
    private readonly ulong epoch;
    private readonly TaskCompletionSource<bool> disposeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task disposal;
    private readonly TaskCompletionSource<bool> initializeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task initialization;
    private CancellationToken initializeCancellation;
    private bool initializeStarted;
    private ErlDriverAsyncAccountingCaptureReply1151? metadata;
    private Task? running;
    private string? appliedAction;
    private bool submitted, applied, appliedSuccessfully, finished, busy, disposing, failed, aborted;
    private Exception? lastFailure;

    internal RuntimeAsyncAccountingCapture1170(RuntimeDriverFrontend422 frontend,
        RuntimeAsyncAccountingFrontendLease1170 frontendLease, ulong epoch)
    {
        this.frontend = frontend;
        this.frontendLease = frontendLease;
        this.epoch = epoch;
        RuntimeAsyncAccountingCodec1170.RequireFrameBudget(frontend.AsyncAccountingExpectedCount1170,
            frontend.AsyncAccountingMaximumFrame1170);
        // Allocate/register first async suspension before any capture RPC.
        disposal = DisposeCore1170Async();
        if (disposal.IsFaulted) disposal.GetAwaiter().GetResult();
        initialization = InitializeCore1170Async();
        if (initialization.IsFaulted) initialization.GetAwaiter().GetResult();
    }

    internal object FrontendIdentity1170 => frontend;
    internal int Count
    {
        get
        {
            lock (gate) return checked((int)RequireMetadata1170().Count);
        }
    }
    internal string Generation1170
    {
        get
        {
            lock (gate) return RequireMetadata1170().Generation;
        }
    }
    internal bool IsOpen1170
    {
        get
        {
            lock (gate) return metadata is not null && !disposing && !failed;
        }
    }

    private ErlDriverAsyncAccountingCaptureReply1151 RequireMetadata1170()
        => metadata ?? throw RuntimeAsyncAccountingCodec1170.Invalid();

    internal Task Initialize1170Async(CancellationToken cancellation)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposing, this);
            if (initializeStarted) throw RuntimeAsyncAccountingCodec1170.Invalid();
            initializeStarted = true;
            initializeCancellation = cancellation;
        }
        initializeRequested.TrySetResult(true);

        return initialization;
    }

    private async Task InitializeCore1170Async()
    {
        if (!await initializeRequested.Task.ConfigureAwait(false)) return;
        CancellationToken cancellation = initializeCancellation;
        byte[] input = ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingCaptureRequest1151(
            ErlDriverAsyncAccountingWireNames1151.Version, ErlDriverAsyncAccountingWireNames1151.Profile, epoch));
        cancellation.ThrowIfCancellationRequested();
        // From this admission cutpoint responses cannot be abandoned. Root has
        // already bound this Capture Task into actual request physical custody.
        lock (gate) submitted = true;
        try
        {
            byte[] response = await frontend.RequestAsyncAccounting1170(
                ErlDriverHostOperation420.AsyncAccountingCapture1151, input).ConfigureAwait(false);
            var captured = RuntimeAsyncAccountingCodec1170.Decode<ErlDriverAsyncAccountingCaptureReply1151>(response);
            RuntimeAsyncAccountingCodec1170.Capture(captured, epoch, frontend.AsyncAccountingExpectedCount1170);
            lock (gate) metadata = captured;
        }
        catch (Exception failure)
        {
            lock (gate)
            {
                failed = true;
                lastFailure = failure;
            }
            throw;
        }
    }

    internal Task<RuntimeMicrostateSnapshot[]> ApplyAsync(RuntimeMicrostateAction action)
    {
        string selected = RuntimeAsyncAccountingCodec1170.Action(action);
        ErlDriverAsyncAccountingCaptureReply1151 captured;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposing || failed, this);
            captured = RequireMetadata1170();
        }
        byte[] input = ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingControlRequest1151(
            captured.Version,
            captured.Profile,
            captured.Epoch,
            captured.Generation,
            captured.Token,
            selected
        ));
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<RuntimeMicrostateSnapshot[]> producer = ApplyCore1170Async(
            start.Task,
            input,
            captured,
            selected
        );
        if (producer.IsFaulted) producer.GetAwaiter().GetResult();
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposing || failed, this);
                if (busy || applied) throw RuntimeAsyncAccountingCodec1170.Invalid();
                applied = true;
                appliedAction = selected;
                busy = true;
                running = producer;
            }
        }
        catch
        {
            start.TrySetResult(false);
            throw;
        }
        start.TrySetResult(true);

        return producer;
    }

    private async Task<RuntimeMicrostateSnapshot[]> ApplyCore1170Async(
        Task<bool> start,
        byte[] input,
        ErlDriverAsyncAccountingCaptureReply1151 captured,
        string action
    )
    {
        if (!await start.ConfigureAwait(false)) return [];
        try
        {
            byte[] response = await frontend.RequestAsyncAccounting1170(
                ErlDriverHostOperation420.AsyncAccountingApply1151, input).ConfigureAwait(false);
            var reply = RuntimeAsyncAccountingCodec1170.Decode<ErlDriverAsyncAccountingApplyReply1151>(response);
            RuntimeMicrostateSnapshot[] rows = RuntimeAsyncAccountingCodec1170.Rows(reply, captured, action);
            lock (gate) appliedSuccessfully = true;

            return rows;
        }
        catch (Exception failure)
        {
            lock (gate)
            {
                failed = true;
                lastFailure = failure;
            }
            try
            {
                await Abort1170Async(failure).ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(failure, cleanup);
            }
            throw;
        }
        finally
        {
            lock (gate) busy = false;
        }
    }

    internal Task FinishAsync(RuntimeMicrostateAction action)
    {
        string selected = RuntimeAsyncAccountingCodec1170.Action(action);
        ErlDriverAsyncAccountingCaptureReply1151 captured;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposing || failed, this);
            captured = RequireMetadata1170();
        }
        byte[] input = ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingControlRequest1151(
            captured.Version,
            captured.Profile,
            captured.Epoch,
            captured.Generation,
            captured.Token,
            selected
        ));
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task producer = FinishCore1170Async(start.Task, input, captured);
        if (producer.IsFaulted) producer.GetAwaiter().GetResult();
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposing || failed, this);
                if (busy || !appliedSuccessfully || finished || appliedAction != selected)
                    throw RuntimeAsyncAccountingCodec1170.Invalid();
                finished = true;
                busy = true;
                running = producer;
            }
        }
        catch
        {
            start.TrySetResult(false);
            throw;
        }
        start.TrySetResult(true);

        return producer;
    }

    private async Task FinishCore1170Async(Task<bool> start, byte[] input,
        ErlDriverAsyncAccountingCaptureReply1151 captured)
    {
        if (!await start.ConfigureAwait(false)) return;
        try
        {
            byte[] response = await frontend.RequestAsyncAccounting1170(
                ErlDriverHostOperation420.AsyncAccountingFinish1151, input).ConfigureAwait(false);
            RuntimeAsyncAccountingCodec1170.Ack(
                RuntimeAsyncAccountingCodec1170.Decode<ErlDriverAsyncAccountingAck1151>(response),
                captured,
                ErlDriverAsyncAccountingWireNames1151.Finish
            );
        }
        catch (Exception failure)
        {
            lock (gate)
            {
                failed = true;
                lastFailure = failure;
            }
            try
            {
                await Abort1170Async(failure).ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(failure, cleanup);
            }
            throw;
        }
        finally
        {
            lock (gate) busy = false;
        }
    }

    private async Task Abort1170Async(Exception failure)
    {
        try
        {
            await frontend.AbortAsyncAccounting1170(failure).ConfigureAwait(false);
        }
        finally
        {
            lock (gate) aborted = frontend.PhysicalAsyncAccountingExit1170.IsCompletedSuccessfully;
        }
    }

    public ValueTask DisposeAsync()
    {
        bool abandonInitialization = false;
        lock (gate)
        {
            disposing = true;
            if (!initializeStarted)
            {
                initializeStarted = true;
                abandonInitialization = true;
            }
        }
        if (abandonInitialization) initializeRequested.TrySetResult(false);
        disposeRequested.TrySetResult(true);

        return new(disposal);
    }

    private async Task DisposeCore1170Async()
    {
        if (!await disposeRequested.Task.ConfigureAwait(false)) return;
        try
        {
            await initialization.ConfigureAwait(false);
        }
        catch { } // Initialization producer preserves its actual primary.
        Task? operation;
        lock (gate) operation = running;
        if (operation is not null)
        {
            try
            {
                await operation.ConfigureAwait(false);
            }
            catch { } // Its actual producer task preserves/reports the primary.
        }
        bool released = false, hadSubmission;
        ErlDriverAsyncAccountingCaptureReply1151? captured;
        lock (gate)
        {
            captured = metadata;
            hadSubmission = submitted;
        }
        Exception? cleanup = null;
        try
        {
            bool gone;
            lock (gate) gone = aborted;
            if (!gone && captured is not null)
            {
                byte[] input = ErlDriverPrimitiveWire420.Encode(new ErlDriverAsyncAccountingReleaseRequest1151(
                    captured.Version,
                    captured.Profile,
                    captured.Epoch,
                    captured.Generation,
                    captured.Token
                ));
                try
                {
                    byte[] response = await frontend.RequestAsyncAccounting1170(
                        ErlDriverHostOperation420.AsyncAccountingRelease1151, input).ConfigureAwait(false);
                    RuntimeAsyncAccountingCodec1170.Ack(
                        RuntimeAsyncAccountingCodec1170.Decode<ErlDriverAsyncAccountingAck1151>(response),
                        captured,
                        ErlDriverAsyncAccountingWireNames1151.Release
                    );
                    released = true;
                }
                catch (Exception failure)
                {
                    cleanup = failure;
                    try
                    {
                        await Abort1170Async(failure).ConfigureAwait(false);
                    }
                    catch (Exception abort)
                    {
                        cleanup = new AggregateException(failure, abort);
                    }
                }
            }
            else if (!gone && hadSubmission)
            {
                Exception failure;
                lock (gate) failure = lastFailure ?? RuntimeAsyncAccountingCodec1170.Invalid();
                try
                {
                    await Abort1170Async(failure).ConfigureAwait(false);
                }
                catch (Exception abort)
                {
                    cleanup = abort;
                }
            }
        }
        catch (Exception failure)
        {
            cleanup = failure;
            try
            {
                await Abort1170Async(failure).ConfigureAwait(false);
            }
            catch (Exception abort)
            {
                cleanup = new AggregateException(failure, abort);
            }
        }
        finally
        {
            // Unknown remote outcome must reach successful-only owned child
            // exit before dropping the real frontend/child lifetime pin.
            if (hadSubmission && !released)
                await frontend.PhysicalAsyncAccountingExit1170.ConfigureAwait(false);
            frontendLease.Dispose();
        }
        if (cleanup is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanup).Throw();
    }
}
