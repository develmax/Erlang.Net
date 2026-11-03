namespace Erlang;

// Internal VM owner only: no public engine close or manual zero-Guest scan.
internal sealed class BinaryProgramOwner598
{
    private readonly object gate = new();
    private readonly HashSet<BinaryProgramBacking598> live = new();
    private readonly NativeResourceDispatcherLease536 normal;
    private readonly TaskCompletionSource buildsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private BinaryProgramBacking598? failures;
    private int activeBuilds;
    private bool closed;
    private Task? close;
    internal ProcessRuntime Runtime598 { get; }

    internal BinaryProgramOwner598(ProcessRuntime runtime)
    {
        Runtime598 = runtime;
        normal = runtime.AcquireNativeResourceDispatcher536();
    }

    internal BinaryProgramOperation598 EnterBuild598()
    {
        var result = new BinaryProgramOperation598(this);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, BinaryArenaNames598.Closed);
            activeBuilds = checked(activeBuilds + 1);
        }

        return result;
    }

    internal void LeaveBuild598()
    {
        lock (gate)
        {
            if (--activeBuilds < 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
            if (closed && activeBuilds == 0) buildsDrained.TrySetResult();
        }
    }

    internal BinaryProgramBacking598 Register598(BinaryProgramArena598 arena)
    {
        var backing = new BinaryProgramBacking598(this, arena);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, BinaryArenaNames598.Closed);
            live.Add(backing);
        }

        return backing;
    }

    internal BinaryProgramLease598 Retain598(BinaryProgramBacking598 backing, BinaryProgramLeaseKind598 kind)
    {
        // Serialize Guest/Host admission with the actual VM closing latch.
        var lease = new BinaryProgramLease598(backing, kind);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || !live.Contains(backing), BinaryArenaNames598.Closed);
            backing.RetainCore598(kind);
        }

        return lease;
    }

    internal BinaryArenaReader598 Reader598(BinaryProgramBacking598 backing)
    {
        var reader = new BinaryArenaReader598(backing);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed || !live.Contains(backing), BinaryArenaNames598.Closed);
            backing.RetainReaderCore598();
        }

        return reader;
    }

    internal void StopAdmission598()
    {
        lock (gate)
        {
            closed = true;
            if (activeBuilds == 0) buildsDrained.TrySetResult();
        }
    }

    internal Task Dispatch598(Action action) => normal.Dispatch536(action);

    internal void Finished598(BinaryProgramBacking598 backing, Exception? failure)
    {
        // No owner/backing monitor while closing canonical resource identity.
        Runtime598.ReferenceResources.ForgetBinaryProgram598(backing);
        lock (gate)
        {
            live.Remove(backing);
            if (failure is not null)
            {
                backing.NextFailure598 = failures;
                failures = backing;
            }
        }
    }

    internal Task Drain598()
    {
        lock (gate)
        {
            if (close is not null) return close;
            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task Run()
            {
                try
                {
                    await DrainCore598().ConfigureAwait(false);
                    completion.TrySetResult();
                }
                catch (Exception error)
                {
                    completion.TrySetException(error);
                }
            }
            if (ExecutionContext.IsFlowSuppressed()) _ = Task.Run(Run);
            else
            {
                using (ExecutionContext.SuppressFlow()) _ = Task.Run(Run);
            }
            closed = true;
            if (activeBuilds == 0) buildsDrained.TrySetResult();

            return close = completion.Task;
        }
    }

    private async Task DrainCore598()
    {
        lock (gate) { }
        await buildsDrained.Task.ConfigureAwait(false);
        BinaryProgramBacking598[] selected;
        lock (gate)
        {
            var pending = new HashSet<BinaryProgramBacking598>(live);
            for (var node = failures; node is not null; node = node.NextFailure598) pending.Add(node);
            selected = pending.ToArray();
        }
        var errors = new List<Exception>();
        try
        {
            foreach (var backing in selected)
                try
                {
                    backing.CloseVm598();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            foreach (var backing in selected)
                try
                {
                    await backing.Join598().ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
        }
        finally
        {
            // A failed physical free retains the real dispatcher participant.
            // It cannot be reported as released while native memory is live.
            if (selected.All(backing => backing.PhysicalFreed598)) normal.Dispose();
        }
        if (errors.Count != 0) throw new AggregateException(errors);
    }
}
