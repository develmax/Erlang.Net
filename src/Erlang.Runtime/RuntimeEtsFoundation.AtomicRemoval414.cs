namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreRemove414(
        ProcessContext context,
        Term identifier,
        Term argument,
        bool take
    )
    {
        Task retirement;
        Term result;
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, true);
            var metadata = coreOptions407.GetValue(table, _ => throw new InvalidOperationException());
            TupleTerm? exact = null;
            if (!take)
            {
                exact = argument as TupleTerm ?? throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
                _ = CoreKey407(exact, metadata.KeyPosition);
            }
            var removed = new List<RuntimeEtsStoredObject>();
            var returned = new List<Term>();
            var next = new Dictionary<long, RuntimeEtsObjectSlot>();
            var order = new LinkedList<RuntimeEtsObjectSlot>();
            foreach (var current in CoreRows420(table))
            {
                context.Cancellation.ThrowIfCancellationRequested();
                TupleTerm row = CoreRead407(current.Payload, context.Cancellation);
                bool matches = take
                    ? EtsCorePlanner407.SameKey(CoreKey407(row, metadata.KeyPosition), argument, metadata.Kind)
                    : row.Equals(exact);
                if (matches)
                {
                    removed.Add(current.Payload);
                    if (take) returned.Add(row);
                }
                else
                {
                    var slot = new RuntimeEtsObjectSlot(current.Id, current.Payload);
                    slot.Link = order.AddLast(slot);
                    next.Add(slot.Id, slot);
                }
            }
            // Construct/copy the returned value before the linearization point.
            result = take ? Cons.From(returned) : Term.A(ErlangBooleanAtoms.True);
            context.Cancellation.ThrowIfCancellationRequested();
            var change420=CoreStageIndex420(table,order,removed,context.Cancellation);
            var prepared=CorePrepareRetirement407(context.Self,change420.Retired);
            CorePublishIndex420(table,change420);
            retirement = CoreBeginRetirement407(prepared);
        }
        // Every materialization lease was scoped by CoreRead407. None survives
        // the guarded swap, and post-commit cancellation must not skip retirement.
        await retirement.ConfigureAwait(false);

        return result;
    }
}
