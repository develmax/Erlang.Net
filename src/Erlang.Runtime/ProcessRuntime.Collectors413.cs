namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly ManagedCollectorSlot413 literalCollector413 = new();
    private readonly ManagedCollectorSlot413 traceCollector413 = new();
    private readonly HashSet<ManagedCollectorRequest413> collectorRequests413 = new();
    private bool collectorsClosed413;

    internal bool HasManagedCollectorProfile413 => ProcessTracing is { } tracing
        && Modules.CollectorLiteralStorage413 == ManagedLiteralStorageModel413.IndependentManagedTerms
        && tracing.CollectorTraceStorage413 is ManagedTraceStorageModel413.ImmortalLegacySessionZero or ManagedTraceStorageModel413.IndependentManagedNamedSessions425;

    private ManagedCollectorOwnership413 CollectorOwnership413 => new(
        Modules.CollectorLiteralStorage413, ProcessTracing.CollectorTraceStorage413);

    private void RequireCollectorActor413(ProcessContext context)
    {
        if (!ReferenceEquals(context.Runtime, this))
            throw new ArgumentException(ManagedCollectorNames413.Foreign, nameof(context));
        RequireEtsFoundationActor(context);
        context.Cancellation.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(collectorsClosed413, this);
        if (!HasManagedCollectorProfile413)
            throw new NotSupportedException(ManagedCollectorNames413.Invariant);
    }

    private void RequireCollectorRole413(ProcessContext context, ManagedCollectorKind413 kind)
    {
        RequireCollectorActor413(context);
        var slot = kind == ManagedCollectorKind413.Literal ? literalCollector413 : traceCollector413;
        if (!ReferenceEquals(slot.Actor, context))
            throw new ErlangException(ManagedCollectorNames413.NotSupported);
    }

    internal bool TrySpawnManagedCollector413(
        ProcessContext parent,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        out ProcessHandle? handle
    )
    {
        handle = null;
        if (function != ManagedCollectorNames413.Start || arguments.Count != 0) return false;
        ManagedCollectorKind413 kind;
        if (module == ManagedCollectorNames413.LiteralModule) kind = ManagedCollectorKind413.Literal;
        else if (module == ManagedCollectorNames413.TraceModule) kind = ManagedCollectorKind413.Trace;
        else return false;
        // Source registry access and hashing are outside runtime.gate (no registry/runtime inverse edge).
        if (Modules.SourceOwner(module, function, 0) is not PreparedBeamImage image
            || !ReferenceEquals(image.Owner, Modules) || !ManagedCollectorSource413.Matches(image, kind, this))
            return false; // General system-spawn remains available, but never grants a role.
        var slot = kind == ManagedCollectorKind413.Literal ? literalCollector413 : traceCollector413;
        object reservation = new();
        lock (gate)
        {
            RequireCollectorActor413(parent);
            if (slot.Reservation is not null || slot.Actor is not null) return false;
            slot.Reservation = reservation;
            slot.Source = image;
        }
        try
        {
            lock (gate)
            {
                handle = SpawnTraced(
                    async child =>
                {
                    lock (gate)
                    {
                        ObjectDisposedException.ThrowIf(collectorsClosed413, this);
                        if (!ReferenceEquals(slot.Reservation, reservation))
                            throw new InvalidOperationException(ManagedCollectorNames413.Invariant);
                        if (!ReferenceEquals(slot.Actor, child))
                            throw new InvalidOperationException(ManagedCollectorNames413.Invariant);
                    }

                    // A hibernate transfer unwinds this invocation but keeps the PID alive.
                    // Only actual actor termination or owner shutdown releases the role.
                    return await BeamRegisterInterpreter.Execute(
                        child,
                        image.Code,
                        function,
                        arguments,
                        image.ExecutionIdentity
                    );
                },
                    parent,
                    false,
                    SequenceSpawnDescription.Module(
                        Term.A(module),
                        Term.A(function),
                        Nil.Value,
                        Nil.Value
                    ),
                    systemProcess: true
                );
                // SpawnTraced's worker first takes this gate. Publish the exact actor
                // before it can be cancelled prior to entering the source body.
                slot.Actor = processes[handle.Pid];
            }

            return true;
        }
        catch
        {
            lock (gate)
                if (ReferenceEquals(slot.Reservation, reservation)) slot.Clear();
            throw;
        }
    }

    internal Term CollectorRelease413(ProcessContext context)
    {
        lock (gate)
        {
            RequireCollectorRole413(context, ManagedCollectorKind413.Literal);

            return CoreModules.Boolean(CollectorOwnership413.SelectNextLiteralRegion());
        }
    }

    internal Term CollectorCheck413(ProcessContext context)
    {
        lock (gate)
        {
            RequireCollectorActor413(context); // Pin has no designated-role guard for check/0.

            return CoreModules.Boolean(CollectorOwnership413.AdvanceTraceCleanupRound());
        }
    }

    private void CollectorProcessEnded413(ProcessContext context)
    {
        // Identity/source roots are cleared first, including if one reply callback fails.
        if (ReferenceEquals(literalCollector413.Actor, context)) literalCollector413.Clear();
        if (ReferenceEquals(traceCollector413.Actor, context)) traceCollector413.Clear();
        List<Exception>? failures = null;
        foreach (var request in collectorRequests413.ToArray())
        {
            try
            {
                if (ReferenceEquals(request.Requester, context)) SettleCollectorRequest413(request, false);
                else if (ReferenceEquals(request.Target, context)) SettleCollectorRequest413(request, true);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }

    internal void AdmitManagedCollector413(ProcessContext context)
    {
        lock (gate) RequireCollectorActor413(context);
    }

    private void CloseCollectors413()
    {
        // Called under runtime.gate on all3shutdown paths, before ReferenceResources close/process teardown.
        // Queued mailbox control objects remain inert, with no AreaId/actor/resource roots after Clear.
        collectorsClosed413 = true;
        literalCollector413.Clear();
        traceCollector413.Clear();
        foreach (var request in collectorRequests413) request.Clear();
        collectorRequests413.Clear();
    }
}
