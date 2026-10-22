namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreUpdateElement414(
        ProcessContext context,
        Term identifier,
        Term key,
        Term updates,
        bool hasDefault,
        Term defaultValue
    )
    {
        RuntimeEtsFoundationTable admitted;
        RuntimeEtsCoreOptions407 metadata;
        lock (gate)
        {
            admitted = CoreResolve407(context, identifier, true);
            metadata = coreOptions407.GetValue(admitted, _ => throw new InvalidOperationException());
            // Pinned /4 validates the default before do_update_element's kind check.
            if (hasDefault && (defaultValue is not TupleTerm initial || initial.Items.Count < metadata.KeyPosition))
                throw new EtsMutationFailure414(EtsMutationNames414.Default);
            if (metadata.Kind is not (EtsCoreTableKind407.Set or EtsCoreTableKind407.OrderedSet))
                throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        }
        while (true)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            RuntimeEtsObjectSlot? original = null;
            TupleTerm value;
            lock (gate)
            {
                var table = CoreResolve407(context, admitted.Handle, true);
                if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                foreach (var slot in CoreRows420(table))
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    var row = CoreRead407(slot.Payload, context.Cancellation);
                    if (!EtsCorePlanner407.SameKey(CoreKey407(row, metadata.KeyPosition), key, metadata.Kind)) continue;
                    original = slot;
                    break;
                }
                // Native lookup precedes list validation; absent /3 returns false.
                if (original is null && !hasDefault) return Term.A(ErlangBooleanAtoms.False);
                if (original is null)
                {
                    var fields = ((TupleTerm)defaultValue).Items.ToArray();
                    fields[(int)metadata.KeyPosition - 1] = key;
                    value = new TupleTerm(fields);
                }
                else value = CoreRead407(original.Payload, context.Cancellation);
            }
            // No read lease or gate survives validation/copy/resource reservation.
            Term rest = updates is TupleTerm pair ? Term.List(pair) : updates;
            var changed = value.Items.ToArray();
            bool any = false;
            while (rest is Cons cell)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (cell.Head is not TupleTerm { Items.Count: 2 } update
                    || update.Items[0] is not Integer position
                    || position.Value < -RuntimeEtsCoreNames407.MaximumSmall64 - 1
                    || position.Value > RuntimeEtsCoreNames407.MaximumSmall64)
                    throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
                if (position.Value == metadata.KeyPosition) throw new EtsMutationFailure414(EtsMutationNames414.KeyPosition);
                if (position.Value < 1 || position.Value > changed.Length) throw new EtsMutationFailure414(EtsMutationNames414.Position);
                changed[(int)position.Value - 1] = update.Items[1];
                any = true;
                rest = cell.Tail;
            }
            if (rest is not Nil) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            RuntimeEtsStoredObject? staged = null;
            RuntimeEtsRetirement? prepared = null;
            bool committed = false;
            Task retirement = Task.CompletedTask;
            try
            {
                // Even unchanged existing objects need no copy. Default [] must insert.
                if (any || original is null)
                    staged = RuntimeEtsStoredObject.Capture(
                        accounting,
                        runtime,
                        new TupleTerm(changed),
                        context.Cancellation
                    );
                lock (gate)
                {
                    var table = CoreResolve407(context, admitted.Handle, true);
                    if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                    RuntimeEtsObjectSlot? current = null;
                    foreach (var slot in CoreRows420(table))
                    {
                        context.Cancellation.ThrowIfCancellationRequested();
                        var row = CoreRead407(slot.Payload, context.Cancellation);
                        if (EtsCorePlanner407.SameKey(CoreKey407(row, metadata.KeyPosition), key, metadata.Kind))
                        {
                            current = slot;
                            break;
                        }
                    }
                    // Writers may rebuild wrappers for unchanged payloads. Compare
                    // monotonic object id AND actual owned payload, never wrapper.
                    bool same = original is null ? current is null
                        : current is not null && current.Id == original.Id && ReferenceEquals(current.Payload, original.Payload);
                    if (!same) continue;
                    if (staged is null) return Term.A(ErlangBooleanAtoms.True);
                    var next = new Dictionary<long, RuntimeEtsObjectSlot>();
                    var order = new LinkedList<RuntimeEtsObjectSlot>();
                    long last = checked(table.LastObjectId + 1);
                    foreach (var old in CoreRows420(table))
                    {
                        context.Cancellation.ThrowIfCancellationRequested();
                        var slot = current is not null && old.Id == current.Id
                            ? new RuntimeEtsObjectSlot(last, staged) : new RuntimeEtsObjectSlot(old.Id, old.Payload);
                        slot.Link = order.AddLast(slot);
                        next.Add(slot.Id, slot);
                    }
                    if (current is null)
                    {
                        var slot = new RuntimeEtsObjectSlot(last, staged);
                        slot.Link = order.AddLast(slot);
                        next.Add(slot.Id, slot);
                    }
                    context.Cancellation.ThrowIfCancellationRequested();
                    var change420=CoreStageIndex420(table,order,current is null?Array.Empty<RuntimeEtsStoredObject>():new[]{current.Payload},context.Cancellation);
                    prepared=CorePrepareRetirement407(context.Self,change420.Retired);
                    CorePublishIndex420(table,change420);
                    table.LastObjectId = last;
                    committed = true;
                    retirement = CoreBeginRetirement407(prepared);
                }
                await retirement.ConfigureAwait(false);

                return Term.A(ErlangBooleanAtoms.True);
            }
            finally
            {
                if (!committed)
                {
                    if (prepared is not null)
                    {
                        prepared.Objects = null;
                        RetirementEnded(prepared, Array.Empty<Exception>());
                    }
                    staged?.Release();
                }
            }
        }
    }
}
