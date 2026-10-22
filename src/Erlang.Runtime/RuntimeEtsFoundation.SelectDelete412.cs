namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreSelectDelete412(ProcessContext context, Term identifier, Term specification)
    {
        RuntimeEtsFoundationTable admitted;
        lock (gate)
        {
            // Writable table/access validation retains native precedence over malformed specs.
            admitted = CoreResolve407(context, identifier, true);
            if (specification is Nil) return Term.I(0);
        }
        MatchSpecProgram program;
        try
        {
            program = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table).Program
                ?? throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        }

        RuntimeEtsObjectSlot[] snapshot;
        RuntimeEtsReadLease?[] leases;
        lock (gate)
        {
            var table = CoreResolve407(context, admitted.Handle, true);
            if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
            snapshot = CoreRows420(table).ToArray();
            leases = new RuntimeEtsReadLease?[snapshot.Length];
            try
            {
                for (int index = 0; index < snapshot.Length; index++)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    leases[index] = snapshot[index].Payload.Acquire();
                }
            }
            catch (Exception original)
            {
                EtsSelectRows409.Release(leases, original);
                throw;
            }
        }
        TupleTerm[] values;
        Exception? failure = null;
        try
        {
            values = EtsSelectRows409.Materialize(leases, context.Cancellation);
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            // No snapshot lease survives a guard callback or a retirement await.
            EtsSelectRows409.Release(leases, failure);
        }

        var matches = new Dictionary<long, RuntimeEtsStoredObject>();
        for (int index = 0; index < snapshot.Length; index++)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            // Pure guard names may resolve mutable HOST callbacks. Evaluate only
            // detached terms outside the runtime/foundation gate.
            MatchSpecResult selected = program.Run(context, values[index], MatchSpecRunMode.Production);
            if (selected.Matched && selected.Value is Atom { Name: ErlangBooleanAtoms.True })
                matches.Add(snapshot[index].Id, snapshot[index].Payload);
        }
        Task retirement;
        long count = 0;
        lock (gate)
        {
            // Preserve original table generation after named deletion/recreation.
            var table = CoreResolve407(context, admitted.Handle, true);
            if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
            context.Cancellation.ThrowIfCancellationRequested();
            if (matches.Count == 0) return Term.I(0);
            var removed = new List<RuntimeEtsStoredObject>();
            var next = new Dictionary<long, RuntimeEtsObjectSlot>();
            var order = new LinkedList<RuntimeEtsObjectSlot>();
            foreach (RuntimeEtsObjectSlot current in CoreRows420(table))
            {
                context.Cancellation.ThrowIfCancellationRequested();
                // Existing writers rebuild wrappers for unchanged rows while
                // preserving their monotonic ID and payload. New/overwritten
                // rows have a different identity and must survive stale matches.
                if (matches.TryGetValue(current.Id, out var payload)
                    && ReferenceEquals(current.Payload, payload))
                {
                    removed.Add(current.Payload);
                    count++;
                }
                else
                {
                    var slot = new RuntimeEtsObjectSlot(current.Id, current.Payload);
                    slot.Link = order.AddLast(slot);
                    next.Add(slot.Id, slot);
                }
            }
            context.Cancellation.ThrowIfCancellationRequested();
            var change420 = CoreStageIndex420(
                table,
                order,
                removed,
                context.Cancellation
            );
            var prepared = CorePrepareRetirement407(context.Self, change420.Retired);
            CorePublishIndex420(table, change420);
            retirement = CoreBeginRetirement407(prepared);
        }
        // No snapshot lease remains; cancellation after commit cannot skip drain.
        await retirement.ConfigureAwait(false);

        return Term.I(count);
    }
}
