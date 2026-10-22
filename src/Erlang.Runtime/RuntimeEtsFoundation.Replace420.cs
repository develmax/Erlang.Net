namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreReplace420(ProcessContext context, Term identifier, Term specification)
    {
        RuntimeEtsFoundationTable admitted;
        RuntimeEtsCoreOptions407 options;
        lock (gate)
        {
            admitted = CoreResolve407(context, identifier, true);
            options = coreOptions407.GetValue(admitted, _ => throw new InvalidOperationException(EtsIndexNames420.InvalidIndex));
            if (options.Kind == EtsCoreTableKind407.Bag) throw new EtsMutationFailure414(EtsCounterNames417.TableType);
            if (specification is Nil) return Term.I(0);
        }
        MatchSpecProgram program = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Table).Program
            ?? throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        foreach (Term item in Cons.Items(specification))
            if (item is not TupleTerm clause || !EtsReplaceKeyProof420.KeepsKey(clause, options.KeyPosition))
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        RuntimeEtsObjectSlot[] snapshot;
        RuntimeEtsReadLease?[] leases;
        lock (gate)
        {
            var table = CoreResolve407(context, admitted.Handle, true);
            if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
            var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);
            snapshot = (options.Kind == EtsCoreTableKind407.OrderedSet
                ? index.Ordered.Reverse().SelectMany(group => group.Rows)
                : index.Buckets.SelectMany(bucket => bucket).SelectMany(group => group.Rows.Reverse())).ToArray();
            leases = new RuntimeEtsReadLease?[snapshot.Length];
            try
            {
                for (int i = 0; i < snapshot.Length; i++)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    leases[i] = snapshot[i].Payload.Acquire();
                }
            }
            catch (Exception error)
            {
                EtsSelectRows409.Release(leases, error);
                throw;
            }
        }
        TupleTerm[] values;
        Exception? materializeFailure = null;
        try
        {
            values = EtsSelectRows409.Materialize(leases, context.Cancellation);
        }
        catch (Exception error)
        {
            materializeFailure = error;
            throw;
        }
        finally
        {
            EtsSelectRows409.Release(leases, materializeFailure);
        }
        var staged = new Dictionary<long, (RuntimeEtsStoredObject Original, RuntimeEtsStoredObject Replacement)>();
        var transferred = new HashSet<RuntimeEtsStoredObject>(ReferenceEqualityComparer.Instance);
        var cleanupFailures = new List<Exception>();
        Exception? executionFailure = null;
        RuntimeEtsRetirement? prepared = null;
        bool committed = false;
        Task retirement = Task.CompletedTask;
        long replaced = 0;
        try
        {
            for (int i = 0; i < snapshot.Length; i++)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                // No lease or foundation gate survives mutable HOST guard evaluation/copy.
                var selected = program.Run(context, values[i], MatchSpecRunMode.Production);
                if (!selected.Matched) continue;
                if (selected.Value is not TupleTerm tuple || tuple.Items.Count < options.KeyPosition)
                    throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
                var copy = RuntimeEtsStoredObject.Capture(
                    accounting,
                    runtime,
                    tuple,
                    context.Cancellation
                );
                try
                {
                    staged.Add(snapshot[i].Id, (snapshot[i].Payload, copy));
                }
                catch
                {
                    copy.Release();
                    throw;
                }
            }
            lock (gate)
            {
                var table = CoreResolve407(context, admitted.Handle, true);
                if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                context.Cancellation.ThrowIfCancellationRequested();
                var order = new LinkedList<RuntimeEtsObjectSlot>();
                var removed = new List<RuntimeEtsStoredObject>();
                long last = table.LastObjectId;
                foreach (var current in CoreRows420(table))
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    RuntimeEtsObjectSlot next;
                    if (staged.TryGetValue(current.Id, out var change) && ReferenceEquals(current.Payload, change.Original))
                    {
                        next = new RuntimeEtsObjectSlot(checked(++last), change.Replacement);
                        removed.Add(current.Payload);
                        transferred.Add(change.Replacement);
                        replaced++;
                    }
                    else next = new RuntimeEtsObjectSlot(current.Id, current.Payload);
                    next.Link = order.AddLast(next);
                }
                if (replaced == 0) return Term.I(0);
                var indexChange = CoreStageIndex420(
                    table,
                    order,
                    removed,
                    context.Cancellation
                );
                prepared = CorePrepareRetirement407(context.Self, indexChange.Retired);
                context.Cancellation.ThrowIfCancellationRequested();
                CorePublishIndex420(table, indexChange);
                table.LastObjectId = last;
                committed = true;
                retirement = CoreBeginRetirement407(prepared);
            }
            // Cancellation after publication cannot skip actual old-root reclamation.
            await retirement.ConfigureAwait(false);

            return Term.I(replaced);
        }
        catch (Exception error)
        {
            executionFailure = error;
            throw;
        }
        finally
        {
            if (!committed && prepared is not null)
                try
                {
                    prepared.Objects = null;
                    RetirementEnded(prepared, Array.Empty<Exception>());
                }
                catch (Exception error)
                {
                    cleanupFailures.Add(error);
                }
            foreach (var change in staged.Values) if (!committed || !transferred.Contains(change.Replacement))
                try
                {
                    change.Replacement.Release();
                }
                catch (Exception error)
                {
                    cleanupFailures.Add(error);
                }
            if (cleanupFailures.Count != 0)
            {
                if (executionFailure is not null) cleanupFailures.Insert(0, executionFailure);
                throw new AggregateException(cleanupFailures);
            }
        }
    }
}
