namespace Erlang;

public sealed partial class ProcessRuntime
{
    private ValueTask<PreparedSequenceDisposeStart> StartSequenceDisposeAsync(List<Exception> failures)
        => Modules.RequiresDirectSequenceShutdown
            ? ValueTask.FromResult(StartSequenceDisposeDirect(failures))
            : StartSequenceDisposeWorkerPathAsync(failures);

    private PreparedSequenceDisposeStart StartSequenceDisposeDirect(List<Exception> failures)
    {
        while (true)
        {
            // Retain the caller's actual module monitor/ThreadStatic permission.
            // No await may migrate this dormant direct path to another thread.
            PreparedSequenceShutdownBatch batch = PrepareSequenceShutdownAsync().GetAwaiter().GetResult();
            Task source = Task.CompletedTask, native = Task.CompletedTask;
            Task? haltWait = null;
            Exception? coreFailure = null;
            bool accepted = false, retry = false;
            try
            {
                lock (gate)
                {
                    if (!disposed && !IsHaltRequested)
                    {
                        if (!AcceptSequenceShutdown(batch)) retry = true;
                        else accepted = true;
                    }
                    if (!retry)
                    {
                        nifShutdownOwnedByPreparedSequence = true;
                        try
                        {
                            haltWait = BeginSequenceDisposeCore();
                        }
                        catch (Exception failure)
                        {
                            coreFailure = failure;
                        }
                    }
                }
                if (!retry)
                {
                    // Original DIRECT calls run with caller M, after releasing R.
                    try
                    {
                        source = Modules.CloseSourceMutationsAsync();
                    }
                    catch (Exception failure)
                    {
                        source = Task.FromException(failure);
                    }
                    try
                    {
                        native = Modules.StartOwnedNifShutdown();
                    }
                    catch (Exception failure)
                    {
                        // No automatic worker may wait for this caller's retained M.
                        // Preserve the real direct shutdown fault in the joined task.
                        native = Task.FromException(failure);
                    }
                }
            }
            catch (Exception failure)
            {
                coreFailure = coreFailure is null ? failure : new AggregateException(coreFailure, failure);
            }
            Task retired;
            try
            {
                retired = accepted ? RetireSequenceShutdownAsync().AsTask() : batch.DisposeAsync().AsTask();
            }
            catch (Exception failure)
            {
                coreFailure = coreFailure is null ? failure : new AggregateException(coreFailure, failure);
                retired = Task.CompletedTask;
            }
            if (retry && coreFailure is null)
            {
                retired.GetAwaiter().GetResult();
                continue;
            }
            ValueTask core = FinishSequenceDisposeAfterRetirement(retired, haltWait, coreFailure);

            return new(source, native, core);
        }
    }

    private async ValueTask FinishSequenceDisposeAfterRetirement(Task retired, Task? haltWait, Exception? failure)
    {
        try
        {
            ProcessTracing.RetirePatternRoots778();
        }
        catch (Exception cleanup)
        {
            failure = failure is null ? cleanup : new AggregateException(failure, cleanup);
        }
        try
        {
            await retired.ConfigureAwait(false);
        }
        catch (Exception cleanup)
        {
            failure = failure is null ? cleanup : new AggregateException(failure, cleanup);
        }
        if (failure is not null)
        {
            bool orphan;
            lock (gate) orphan = processes.Count != 0;
            if (orphan && !IsHaltRequested)
                try
                {
                    RequestInternalTerminationFailureHalt();
                }
                catch (Exception cleanup)
                {
                    failure = new AggregateException(failure, cleanup);
                }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        await FinishSequenceDisposeDrains(haltWait).ConfigureAwait(false);
    }

    private async ValueTask<PreparedSequenceDisposeStart> StartSequenceDisposeWorkerPathAsync(List<Exception> failures)
    {
        while (true)
        {
            PreparedSequenceShutdownBatch batch = await PrepareSequenceShutdownAsync().ConfigureAwait(false);
            Task source = Task.CompletedTask, native = Task.CompletedTask;
            Task? haltWait = null;
            Exception? coreFailure = null;
            bool accepted = false, direct = false, retry = false;
            try
            {
                lock (gate)
                {
                    if (!disposed && !IsHaltRequested)
                    {
                        if (!AcceptSequenceShutdown(batch)) retry = true;
                        else accepted = true;
                    }
                    if (!retry)
                    {
                        direct = Modules.RequiresDirectSequenceShutdown;
                        nifShutdownOwnedByPreparedSequence = true;
                        if (!direct) StartModuleShutdown();
                        try
                        {
                            haltWait = BeginSequenceDisposeCore();
                        }
                        catch (Exception failure)
                        {
                            coreFailure = failure;
                        }
                    }
                }
                // A dormant caller retaining module M uses its ORIGINAL direct
                // close path after R is released. No worker waits for that M.
                if (!retry && direct) StartModuleShutdown();
            }
            catch (Exception failure)
            {
                coreFailure = coreFailure is null ? failure : new AggregateException(coreFailure, failure);
            }
            finally
            {
                try
                {
                    if (accepted) await RetireSequenceShutdownAsync().ConfigureAwait(false);
                    else await batch.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    coreFailure = coreFailure is null ? failure : new AggregateException(coreFailure, failure);
                }
            }
            if (retry && coreFailure is null) continue;
            ValueTask core = FinishSequenceDisposeAfterRetirement(Task.CompletedTask, haltWait, coreFailure);

            return new(source, native, core);

            void StartModuleShutdown()
            {
                try
                {
                    source = Modules.CloseSourceMutationsAsync();
                }
                catch (Exception failure)
                {
                    source = Task.FromException(failure);
                }
                try
                {
                    native = Modules.StartOwnedNifShutdown();
                }
                catch (Exception failure)
                {
                    if (direct) native = Task.FromException(failure);
                    else
                    {
                        failures.Add(failure);
                        native = Task.CompletedTask;
                        nifShutdownOwnedByPreparedSequence = false;
                        StartNifShutdown();
                    }
                }
            }
        }
    }
}
