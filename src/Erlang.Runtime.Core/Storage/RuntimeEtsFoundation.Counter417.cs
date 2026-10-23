namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreUpdateCounter417(
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
            // Pinned /4 validates the default before do_update_counter's kind check.
            if (hasDefault && (defaultValue is not TupleTerm initial || initial.Items.Count < metadata.KeyPosition))
                throw new EtsMutationFailure414(EtsMutationNames414.Default);
            if (metadata.Kind is not (EtsCoreTableKind407.Set or EtsCoreTableKind407.OrderedSet))
                throw new EtsMutationFailure414(EtsCounterNames417.TableType);
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
                // Native lookup precedes list validation; absent /3 reports the source badkey cause.
                if (original is null && !hasDefault) throw new EtsMutationFailure414(EtsCounterNames417.BadKey);
                if (original is null)
                {
                    var fields = ((TupleTerm)defaultValue).Items.ToArray();
                    fields[(int)metadata.KeyPosition - 1] = key;
                    value = new TupleTerm(fields);
                }
                else value = CoreRead407(original.Payload, context.Cancellation);
            }
            // Source validates every operation against the original tuple before mutation.
            TupleTerm changed = EtsCounterPlan417.Apply(
                value,
                metadata.KeyPosition,
                updates,
                context.Cancellation,
                out Term result,
                out bool any
            );
            RuntimeEtsStoredObject? staged = null;
            RuntimeEtsRetirement? prepared = null;
            bool committed = false;
            Task retirement = Task.CompletedTask;
            try
            {
                // An empty operation list preserves an existing object; default [] still inserts.
                if (any || original is null)
                    staged = RuntimeEtsStoredObject.CaptureCompressed438(
                        accounting,
                        runtime,
                        changed,
                        context.Cancellation,
                        compressed: metadata.Compressed,
                        keyPosition: metadata.KeyPosition
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
                    if (staged is null) return result;
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
                    var change420 = CoreStageIndex420(
                        table,
                        order,
                        current is null ? Array.Empty<RuntimeEtsStoredObject>() : new[] { current.Payload },
                        context.Cancellation
                    );
                    prepared = CorePrepareRetirement407(context.Self, change420.Retired);
                    CorePublishIndex420(table, change420);
                    table.LastObjectId = last;
                    committed = true;
                    retirement = CoreBeginRetirement407(prepared);
                }
                await retirement.ConfigureAwait(false);

                return result;
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
