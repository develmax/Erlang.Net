namespace Erlang;

// The registered trap owns this producer before any child capture is admitted.
// Its completion is derived from actual child, dispatcher and request joins.
internal sealed class RuntimeMicrostateOperation1171
{
    private enum StartMode
    {
        Abandoned,
        Failed,
        Local,
        Remote
    }

    private enum FailureSlot
    {
        Source,
        Capture,
        BatchAbort,
        RequestAbort,
        TailReject,
        Release,
        TailJoin,
        BatchJoin,
        RequestJoin,
        Count
    }

    private readonly TaskCompletionSource<StartMode> start = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<Exception?> sourceReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Exception?[] errors = new Exception?[(int)FailureSlot.Count];
    private readonly OperationCanceledException unpublished = new();
    private Exception? preparationFailure;
    private int ownLane;
    private bool published;

    internal Task<RuntimeAsyncAccountingCapture1170?>? CaptureTask { get; set; }
    internal RuntimeMicrostateRequest? Request { get; private set; }
    internal RuntimeSchedulerAuxiliaryBatch420? Batch { get; private set; }
    internal RuntimeMicrostateSourceTail1149? Tail { get; set; }
    internal Task<Exception?> SourceReady => sourceReady.Task;
    internal Task Completion { get; }

    internal RuntimeMicrostateOperation1171()
    {
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            Completion = Run();
            if (Completion.IsFaulted) Completion.GetAwaiter().GetResult();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal void Attach(RuntimeMicrostateRequest request, RuntimeSchedulerAuxiliaryBatch420 batch)
    {
        if (start.Task.IsCompleted || Request is not null)
            throw new InvalidOperationException(RuntimeMicrostateOperationNames1171.Phase);

        Request = request;
        Batch = batch;
    }

    internal void MarkPublished(int lane)
    {
        ownLane = lane;
        published = true;
    }

    internal void StartLocal() => start.TrySetResult(StartMode.Local);

    internal void StartRemote() => start.TrySetResult(StartMode.Remote);

    internal void AbandonIfNotStarted() => start.TrySetResult(StartMode.Abandoned);

    internal void Fail(Exception error)
    {
        preparationFailure = error;
        start.TrySetResult(StartMode.Failed);
    }

    internal void AbortPreparation(Exception error)
        => Request?.AbortStatisticsTrap(error);

    private async Task Run()
    {
        StartMode mode = await start.Task.ConfigureAwait(false);
        RuntimeAsyncAccountingCapture1170? capture = null;
        bool sourceFailed = false;
        Task? failedTail = null;
        Task? failedBatch = null;
        Task? failedRequest = null;
        try
        {
            if (CaptureTask is { } pending)
                capture = await pending.ConfigureAwait(false);

            if (mode == StartMode.Failed)
                throw preparationFailure ?? new InvalidOperationException(RuntimeMicrostateOperationNames1171.Phase);
            if (mode == StartMode.Remote)
            {
                if (capture is null || Request is null || Tail is null || !published)
                    throw new InvalidOperationException(RuntimeMicrostateOperationNames1171.Phase);

                RuntimeMicrostateSnapshot[] rows = await capture.ApplyAsync(Request.Action).ConfigureAwait(false);
                Request.ApplyRemoteSnapshots1171(rows);
                Request.ApplyLocalUnmanaged1171();
                Tail.QueueFirst();
                await Tail.FirstCompletion.ConfigureAwait(false);
                await capture.FinishAsync(Request.Action).ConfigureAwait(false);
                Tail.QueueSecond();
                await Tail.Completion.ConfigureAwait(false);
            }
            sourceReady.TrySetResult(null);
        }
        catch (Exception error)
        {
            sourceFailed = true;
            errors[(int)FailureSlot.Source] = error;
            sourceReady.TrySetResult(error);
        }
        finally
        {
            Exception? abort = sourceFailed
                ? errors[(int)FailureSlot.Source]
                : mode == StartMode.Abandoned ? unpublished : null;
            if (abort is not null)
            {
                if (Tail is { } tail)
                {
                    try
                    {
                        tail.Reject(abort);
                    }
                    catch (Exception error)
                    {
                        errors[(int)FailureSlot.TailReject] = error;
                    }
                }
                if (Batch is { } batch)
                {
                    try
                    {
                        if (batch.State == RuntimeSchedulerAuxiliaryState420.Prepared)
                            batch.Abort(abort);
                        else if (published && Tail is null)
                            batch.Work[ownLane - ProcessSchedulerNames.First].Fail(abort);
                    }
                    catch (Exception error)
                    {
                        errors[(int)FailureSlot.BatchAbort] = error;
                    }
                }
                try
                {
                    Request?.AbortStatisticsTrap(abort);
                }
                catch (Exception error)
                {
                    errors[(int)FailureSlot.RequestAbort] = error;
                }
            }

            // Recover the admitted capture even when the caller failed before
            // observing its result. Never abandon the correlated child reply.
            if (capture is null && CaptureTask is { } admitted)
            {
                try
                {
                    capture = await admitted.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    errors[(int)FailureSlot.Capture] = error;
                }
            }
            if (capture is not null)
            {
                try
                {
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    errors[(int)FailureSlot.Release] = error;
                }
            }
            if (Tail is { } actualTail)
            {
                try
                {
                    await actualTail.Completion.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failedTail = actualTail.Completion;
                    errors[(int)FailureSlot.TailJoin] = error;
                }
            }
            if (Batch is { } actualBatch)
            {
                try
                {
                    await actualBatch.Completion.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    if (!(mode == StartMode.Abandoned && !published && IsOnlyUnpublished(actualBatch.Completion, error)))
                    {
                        failedBatch = actualBatch.Completion;
                        errors[(int)FailureSlot.BatchJoin] = error;
                    }
                }
            }
            if (Request is { } request)
            {
                try
                {
                    await request.Completion.ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    if (!(mode == StartMode.Abandoned && !published && IsOnlyUnpublished(request.Completion, error)))
                    {
                        failedRequest = request.Completion;
                        errors[(int)FailureSlot.RequestJoin] = error;
                    }
                }
            }
        }

        var failures = new StatisticsTrapFailures();
        foreach (Exception? error in errors)
            if (error is not null) failures.Add(error);
        AddTaskFailures(failures, failedTail);
        AddTaskFailures(failures, failedBatch);
        AddTaskFailures(failures, failedRequest);
        failures.ThrowIfAny();
    }

    private static void AddTaskFailures(StatisticsTrapFailures failures, Task? task)
    {
        if (task?.Exception is not { } aggregate) return;
        foreach (Exception error in aggregate.InnerExceptions) failures.Add(error);
    }

    private bool IsOnlyUnpublished(Task completion, Exception observed)
    {
        if (completion.Exception is { } aggregate)
            return aggregate.InnerExceptions.Count != 0
                && aggregate.InnerExceptions.All(error => ReferenceEquals(error, unpublished));

        return ReferenceEquals(observed, unpublished);
    }
}
