namespace Erlang;

internal sealed class AllocatorGatherOwner434(ProcessRuntime runtime, object gate)
{
    private readonly HashSet<AllocatorGatherRequest434> pending = new(ReferenceEqualityComparer.Instance);
    private bool closed;
    private readonly List<Exception> failures = new();

    internal void Publish(AllocatorGatherRequest434 request)
    {
        // Caller owns the actual runtime gate. Preparation can throw before any
        // real queued work is admitted; caller never abandons admitted work.
        ObjectDisposedException.ThrowIf(closed, this);
        pending.EnsureCapacity(checked(pending.Count + 1));
        failures.EnsureCapacity(checked(failures.Count + pending.Count + 1));
        request.Batch = runtime.PrepareAllocatorAux434(lane =>
        {
            if (lane != ProcessSchedulerNames.First) return;
            AllocatorBlockWindow434[] window = runtime.CaptureAllocatorWindow434(request.Allocator);
            Term message = request.Carriers ? AllocatorGatherTerms434.Carriers(request, window) : AllocatorGatherTerms434.Histograms(request, window);
            runtime.SendSystem(request.Recipient!, message, Term.A(AllocatorGatherNames434.System));
        });
        bool observed = false;
        try
        {
            bool suppress = !ExecutionContext.IsFlowSuppressed();
            AsyncFlowControl flow = suppress ? ExecutionContext.SuppressFlow() : default;
            try
            {
                _ = Observe(request);
                observed = true;
            }
            finally
            {
                if (suppress) flow.Undo();
            }
            pending.Add(request);
            request.Batch.Publish();
        }
        catch (Exception error)
        {
            if (request.Batch.State == RuntimeSchedulerAuxiliaryState420.Prepared) request.Batch.Abort(error);
            if (!observed)
            {
                request.Clear();
                request.PhysicalDone.TrySetException(error);
                _ = request.PhysicalDone.Task.Exception;
            }
            throw;
        }
    }

    private async Task Observe(AllocatorGatherRequest434 request)
    {
        Exception? failure = null;
        try
        {
            await request.Batch!.Completion.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failure = error;
        }
        lock (gate)
        {
            pending.Remove(request);
            request.Clear();
            if (failure is not null) failures.Add(failure);
            if (failure is null) request.PhysicalDone.TrySetResult();
            else
            {
                request.PhysicalDone.TrySetException(failure);
                _ = request.PhysicalDone.Task.Exception;
            }
        }
    }

    internal Task Close()
    {
        Task result = Task.WhenAll(pending.Select(request => request.PhysicalDone.Task));
        Task complete = failures.Count == 0 ? result : Task.WhenAll(result, Task.FromException(new AggregateException(failures)));
        closed = true;

        return complete;
    }

    internal Task ProcessRetirement(ProcessContext context) => Task.WhenAll(pending.Where(request => ReferenceEquals(request.Origin, context)).Select(request => request.PhysicalDone.Task));

    internal bool Drained => pending.Count == 0 && failures.Count == 0;
}
