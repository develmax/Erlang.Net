namespace Erlang;

// Unpublished until the complete native-port frontend/close hooks are composed.
// One shared runtime gate protects actual parent namespace state. IPC is always
// outside it; authenticated nested native registrations do not wait on mutation.
internal sealed partial class RuntimeDdllParent422
{
    private readonly ProcessRuntime runtime;
    private readonly object gate;
    private readonly DriverParentPeerTransport422 transport;
    private readonly Func<DdllParentRow422, Task> killPorts;
    private readonly LinkedList<DdllParentRow422> drivers = new();
    private readonly Dictionary<ulong, DdllParentRow422> identities = new();
    private readonly Queue<DdllParentRow422> retiring = new();
    private readonly SemaphoreSlim wake = new(0);
    private Task worker = Task.CompletedTask;
    private bool workerStarted;
    private TaskCompletionSource? mutation;
    private ulong nextIdentity;
    private bool closing;

    internal RuntimeDdllParent422(
        ProcessRuntime runtime,
        object gate,
        DriverParentPeerTransport422 transport,
        Func<DdllParentRow422, Task> killPorts
    )
    {
        this.runtime = runtime;
        this.gate = gate;
        this.transport = transport;
        this.killPorts = killPorts;
    }

    internal void StartWorker()
    {
        if (workerStarted) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            worker = RetireLoop();
            workerStarted = true;
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal Task Completion => worker;

    private TaskCompletionSource BeginMutation()
    {
        if (PendingMutation438 is not null) throw new InvalidOperationException(DriverParentWireNames422.Invalid);

        return mutation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void EndMutation(TaskCompletionSource expected)
    {
        if (!ReferenceEquals(mutation, expected)) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
        mutation = null;
        expected.TrySetResult();
    }

    private async ValueTask<T> Stable<T>(ProcessContext context, Func<T> operation)
    {
        while (true)
        {
            Task? pending;
            lock (gate)
            {
                runtime.RequireDriverParentActor422(context);
                ObjectDisposedException.ThrowIf(closing, this);
                pending = PendingMutation438;
                if (pending is null) return operation();
            }
            await pending.WaitAsync(context.Cancellation).ConfigureAwait(false);
        }
    }

    internal ValueTask<Term> LoadedDrivers(ProcessContext context) => Stable(context, LoadedDriversCore);

    internal ValueTask<Term> Info(ProcessContext context, Term name, Term item)
    {
        runtime.RequireDriverParentActor422(context);
        ErlDriverName420 parsed = ErlDriverName420.Parse(name, context.Cancellation);

        return Stable(context, () => InfoCore(parsed, item));
    }

    internal ValueTask<Term> Monitor(ProcessContext context, Term type, Term description)
        => Stable<Term>(context, () => MonitorCore(
            context.Self,
            type,
            description,
            context.Cancellation
        ));

    internal ValueTask<Term> Demonitor(ProcessContext context, Term reference)
        => Stable(context, () => DemonitorCore(context.Self, reference));

    private void QueueRetirement(DdllParentRow422 row)
    {
        if (row.Linked || row.RetirementQueued) return;
        if (!closing && row.State is not (DdllParentStateKind422.Unloading or DdllParentStateKind422.ForceUnloading
            or DdllParentStateKind422.Reloading or DdllParentStateKind422.ForceReloading)) return;
        if (row.Ports.Count != 0 || row.Processes.Any(entry => entry.Kind == DdllParentReferenceKind422.Loaded && !entry.Deferred)) return;
        retiring.Enqueue(row);
        row.RetirementQueued = true;
        wake.Release();
    }

    internal ulong RegisterLinked(ErlDriverLinkedEntryRequest420 request, ErlDriverLinkedScope438 scope)
    {
        lock (gate)
        {
            // Guest admission is closed independently. Authenticated native
            // finish/TLS cleanup may still add/remove a transient linked entry
            // while its exact image remains physically owned.
            RequireLinkedScope438(scope, requireRoot: true);
            if (request.OwnerImage != scope.OwnerImage
                || !identities.TryGetValue(request.OwnerImage, out var owner) || owner.Linked)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            ErlDriverName420 name = ErlDriverName420.Parse(new BitString(request.Name), CancellationToken.None);
            ulong identity = checked(nextIdentity + 1);
            var row = new DdllParentRow422(
                identity,
                name,
                null,
                true,
                request.OwnerImage,
                false
            )
            { State = DdllParentStateKind422.Healthy, NativeFlags = request.Flags };
            identities.Add(identity, row);
            try
            {
                drivers.AddFirst(row.Node);
            }
            catch
            {
                identities.Remove(identity);
                throw;
            }
            nextIdentity = identity;

            return identity;
        }
    }

    internal int RemoveLinked(ulong identity, ErlDriverLinkedScope438 scope)
    {
        lock (gate)
        {
            RequireLinkedScope438(scope, requireRoot: false);
            if (!identities.TryGetValue(identity, out var row)) return 0;
            if (!row.Linked) return -1;
            if (row.Ports.Count != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
            row.Node.List?.Remove(row.Node);
            identities.Remove(identity);

            return 1;
        }
    }

    internal int LockDriver(ulong identity)
    {
        lock (gate)
        {
            if (!identities.TryGetValue(identity, out var row) || row.Linked) return -1;
            NotifyAll(
                row,
                kind => kind != DdllParentReferenceKind422.Loaded,
                DdllParentAtoms422.Up,
                Term.A(DdllParentAtoms422.Permanent)
            );
            row.Processes.Clear();
            row.State = DdllParentStateKind422.Permanent;
            row.ReloadPath = null;

            return 0;
        }
    }
}
