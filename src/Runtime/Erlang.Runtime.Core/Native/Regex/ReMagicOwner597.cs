namespace Erlang;

internal sealed class ReMagicOwner597
{
    private readonly object gate = new();
    private readonly ReRelease411 release;
    private readonly Action<ReCode411> retired;
    private readonly HashSet<ReMagicBacking597> live = new();
    private readonly NativeResourceDispatcherLease536 normal;
    private bool closing;
    private ReMagicBacking597? failed;
    private Task? close;
    internal ProcessRuntime Runtime597 { get; }

    internal ReMagicOwner597(ProcessRuntime runtime, ReRelease411 release, Action<ReCode411> retired)
    {
        Runtime597 = runtime;
        this.release = release;
        this.retired = retired;
        normal = runtime.AcquireNativeResourceDispatcher536();
    }

    internal ReMagicBacking597 Create597(ReCode411 code, nint pointer)
    {
        var backing = new ReMagicBacking597(this, code, pointer);
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, ReMagicNames597.Closed);
                live.Add(backing);
            }

            return backing;
        }
        catch
        {
            backing.AbandonPrefix597();
            throw;
        }
    }

    internal void AbandonUnpublished597(ReMagicBacking597 backing)
    {
        backing.AbandonPrefix597();
        lock (gate) live.Remove(backing);
    }

    internal Task Dispatch597(Action body) => normal.Dispatch536(body);

    internal void FreeNativeCode597(nint pointer) => release(pointer);

    internal void Finished597(ReMagicBacking597 backing, Exception? recordedFailure)
    {
        Exception? failure = null;
        try
        {
            Runtime597.ReferenceResources.ForgetRegex597(backing);
        }
        catch (Exception error)
        {
            failure = error;
        }
        // The code really was freed even if canonical-index removal failed.
        retired(backing.CodeOwner597);
        if (failure is null)
        {
            lock (gate)
            {
                live.Remove(backing);
                if (recordedFailure is not null)
                {
                    backing.NextFailure597 = failed;
                    failed = backing;
                }
            }
        }
        else System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal bool HasGuestRoots597
    {
        get
        {
            lock (gate) return live.Any(backing => backing.HasGuestRoots597);
        }
    }

    internal void RequireAdmissionGate599()
    {
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException(ReMagicNames597.Invalid);
    }

    internal ReMagicLease597 RetainGuest599(ReMagicBacking597 backing)
    {
        var lease = new ReMagicLease597(backing, ReMagicLeaseKind597.Guest);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, ReMagicNames597.Closed);
            if (!live.Contains(backing)) throw new InvalidOperationException(ReMagicNames597.Invalid);
            backing.CommitGuest599();
        }

        return lease;
    }

    internal void BeginManualClose599()
    {
        lock (gate)
        {
            if (closing) return;
            if (live.Any(backing => backing.HasGuestRoots597))
                throw new InvalidOperationException(ReMagicNames597.ActiveDispose);
            // Same gate as RetainGuest599: either a Guest committed before
            // this decision and close refuses, or all later Guest admissions fail.
            closing = true;
        }
    }

    internal void BeginUnpublishedClose599()
    {
        lock (gate)
        {
            if (live.Count != 0 || failed is not null)
                throw new InvalidOperationException(ReMagicNames597.UnpublishedDispose599);
            closing = true;
        }
    }

    internal void ReleaseUnpublishedDispatcher599()
    {
        // No allocations, queue or await are required for a genuinely empty
        // fresh owner. Disposal happens outside engine/runtime/owner gates.
        BeginUnpublishedClose599();
        normal.Dispose();
    }

    internal bool AllPhysicalFreed597
    {
        get
        {
            lock (gate) return live.All(backing => backing.PhysicalFreed597);
        }
    }

    internal Task Close597()
    {
        lock (gate)
        {
            if (close is not null) return close;
            // Snapshot before publishing closing; OOM cannot leave a waitable
            // half-close. No new backing is admitted after this lock exits.
            var pending = new HashSet<ReMagicBacking597>(live);
            for (ReMagicBacking597? node = failed; node is not null; node = node.NextFailure597) pending.Add(node);
            ReMagicBacking597[] selected = pending.ToArray();
            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task Run()
            {
                try
                {
                    await CloseCore597(selected).ConfigureAwait(false);
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
            closing = true;

            return close = completion.Task;
        }
    }

    private async Task CloseCore597(ReMagicBacking597[] selected)
    {
        // Join owner transition; never retain owner monitor across waits.
        lock (gate) { }
        var failures = new List<Exception>();
        try
        {
            foreach (var backing in selected)
                try
                {
                    backing.CloseVm597();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            foreach (var backing in selected)
                try
                {
                    await backing.Join597().ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        finally
        {
            normal.Dispose();
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
