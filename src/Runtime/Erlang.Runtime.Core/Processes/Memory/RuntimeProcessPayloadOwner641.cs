namespace Erlang;

internal sealed class RuntimeProcessPayloadOwner641(ProcessRuntime runtime)
{
    private readonly object gate = new();
    private readonly HashSet<RuntimeProcessPayloadWork641> live = new(ReferenceEqualityComparer.Instance);
    private RuntimeProcessPayloadWork641? failures;
    private bool stopped, drained;
    private Task? drain;
    internal ProcessRuntime Runtime641 { get; } = runtime;

    internal RuntimeProcessPayloadWork641 Reserve641(ProcessContext? context, bool derived)
    {
        var work = new RuntimeProcessPayloadWork641(this, context);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(drained || stopped && !derived, RuntimeProcessPayloadNames641.Closed);
            if (context is not null)
            {
                if (!ReferenceEquals(context.Runtime, Runtime641))
                    throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
                ObjectDisposedException.ThrowIf(context.ProcessPayloadAdmissionClosed641, RuntimeProcessPayloadNames641.Closed);
            }
            live.Add(work);
        }

        return work;
    }

    internal void Stop641()
    {
        lock (gate) stopped = true;
        // Do NOT fabricate end-of-use for captures/callbacks/continuations.
        // Real owner finally/table removal must end its specific record.
    }

    internal void Finished641(RuntimeProcessPayloadWork641 work, Exception? failure)
    {
        lock (gate)
        {
            live.Remove(work);
            if (failure is not null)
            {
                work.NextFailure641 = failures;
                failures = work;
            }
        }
    }

    internal bool HasContextRetirement641(ProcessContext context)
    {
        lock (gate)
        {
            foreach (var work in live)
                if (ReferenceEquals(work.Context641, context)) return true;
            for (var item = failures; item is not null; item = item.NextFailure641)
                if (ReferenceEquals(item.Context641, context)) return true;

            return false;
        }
    }

    internal void SealContext641(ProcessContext context)
    {
        lock (gate) context.ProcessPayloadAdmissionClosed641 = true;
    }

    internal Task JoinContext641(ProcessContext context)
    {
        RuntimeProcessPayloadWork641[] snapshot;
        lock (gate)
        {
            var selected = new HashSet<RuntimeProcessPayloadWork641>(
                live.Where(work => ReferenceEquals(work.Context641, context)), ReferenceEqualityComparer.Instance);
            for (var item = failures; item is not null; item = item.NextFailure641)
                if (ReferenceEquals(item.Context641, context)) selected.Add(item);
            snapshot = selected.ToArray();
        }
        if (ExecutionContext.IsFlowSuppressed()) return JoinSelected641(snapshot);
        using (ExecutionContext.SuppressFlow()) return JoinSelected641(snapshot);
    }

    internal Task Drain641()
    {
        lock (gate)
        {
            if (drain is not null) return drain;
            stopped = true;
            if (ExecutionContext.IsFlowSuppressed()) return drain = DrainCore641();
            using (ExecutionContext.SuppressFlow()) return drain = DrainCore641();
        }
    }

    private async Task DrainCore641()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        var errors = new List<Exception>();
        while (true)
        {
            RuntimeProcessPayloadWork641[] snapshot;
            lock (gate)
            {
                snapshot = live.ToArray();
                if (snapshot.Length == 0)
                {
                    drained = true;
                    for (var item = failures; item is not null; item = item.NextFailure641)
                        if (item.Failure641 is { } failure) errors.Add(failure);
                    break;
                }
            }
            try
            {
                await JoinSelected641(snapshot).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (errors.Count != 0) throw new AggregateException(errors);
    }

    private static async Task JoinSelected641(RuntimeProcessPayloadWork641[] snapshot)
    {
        // Source/foreign actor scheduler is never a cleanup dependency.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        var errors = new List<Exception>();
        foreach (var work in snapshot)
            try
            {
                await work.Join641().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        if (errors.Count != 0) throw new AggregateException(errors);
    }
}
