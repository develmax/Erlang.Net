using System.Runtime.CompilerServices;

namespace Erlang;

// Internal semantic kernel only. No native provider/module registration.
internal sealed partial class RuntimeEtsFoundation
{
    private readonly ConditionalWeakTable<RuntimeEtsFoundationTable, RuntimeEtsCoreOptions407> coreOptions407 = new();

    internal Term CoreNew407(ProcessContext context, Term name, Term options)
    {
        if (name is not Atom atom) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        RuntimeEtsNewHeirPlan419 requested = RuntimeEtsNewHeirPlan419.Parse(runtime, options, context.Cancellation);
        RuntimeEtsCoreOptions407 parsed = RuntimeEtsCoreOptions407.Parse(requested.Options, context.Cancellation);
        RuntimeEtsStoredObject? staged = null;
        bool consumed = false;
        try
        {
            if (requested.Heir is not null && requested.Gift is not null)
                staged = RuntimeEtsStoredObject.Capture(
                    accounting,
                    runtime,
                    Term.Tuple(requested.Gift),
                    context.Cancellation
                );
            lock (gate)
            {
                RequireActor(context);
                RuntimeEtsHeir419? heir = requested.Heir is { } pid && runtime.ReadEtsHeirProcess419(pid) is { } generation
                    ? new(pid, new(generation), staged) : null;
                ResourceReferenceTerm handle;
                try
                {
                    handle = CreateOwnerTable(
                        context,
                        atom,
                        parsed.Named,
                        parsed.Protection
                    );
                }
                catch (InvalidOperationException error) when (error.Message == RuntimeEtsFoundationNames.NameInUse)
                {
                    throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.AlreadyExists);
                }
                var table = Resolve(handle);
                try
                {
                    coreOptions407.Add(table, parsed);
                    CorePublishIndex420(
                        table,
                        CoreStageIndex420(
                            table,
                            Array.Empty<RuntimeEtsObjectSlot>(),
                            Array.Empty<RuntimeEtsStoredObject>(),
                            context.Cancellation
                        )
                    );
                }
                catch
                {
                    _ = Detach(table);
                    throw;
                } // Still empty and no heir root is published on this failure path.
                table.Heir419 = heir;
                consumed = heir?.Data is not null;

                return parsed.Named ? atom : handle;
            }
        }
        finally
        {
            if (!consumed) staged?.Release();
        }
    }

    internal Term CoreWhereis407(ProcessContext context, Term name)
    {
        lock (gate)
        {
            RequireActor(context);
            if (name is not Atom atom) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);

            return names.TryGetValue(atom.Name, out var table) && IsTableVisible419(table)
                ? table.Handle : Term.A(CommonAtomNames.Undefined);
        }
    }

    private RuntimeEtsFoundationTable CoreResolve407(ProcessContext context, Term identifier, bool write)
    {
        RequireActor(context);
        if (identifier is not (Atom or ReferenceTerm)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Type);
        RuntimeEtsFoundationTable table;
        try
        {
            table = Resolve(identifier);
        }
        catch (InvalidOperationException error) when (error.Message == RuntimeEtsFoundationNames.InvalidTable)
        {
            throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
        }
        try
        {
            RequireAccess(context, table, write);
        }
        catch (InvalidOperationException error) when (error.Message == RuntimeEtsFoundationNames.AccessDenied)
        {
            throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Access);
        }
        if (!coreOptions407.TryGetValue(table, out _)) throw new NotSupportedException(RuntimeEtsCoreNames407.HostTable);

        return table;
    }

    private static TupleTerm CoreRead407(RuntimeEtsStoredObject value, CancellationToken cancellation)
    {
        using var lease = value.Acquire();

        return (TupleTerm)lease.Materialize(cancellation);
    }

    private static Term CoreKey407(TupleTerm value, long position)
    {
        if (position <= 0 || position > value.Items.Count) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.BadObject);

        return value.Items[(int)position - 1];
    }

    private static List<TupleTerm> CoreValidate407(Term objects, long position, CancellationToken cancellation)
    {
        var result = new List<TupleTerm>();
        if (objects is TupleTerm tuple)
        {
            _ = CoreKey407(tuple, position);
            result.Add(tuple);

            return result;
        }
        Term rest = objects;
        while (rest is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            if (cell.Head is not TupleTerm row) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.BadObject);
            _ = CoreKey407(row, position);
            result.Add(row);
            rest = cell.Tail;
        }
        if (rest is not Nil) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.BadObject);
        cancellation.ThrowIfCancellationRequested();

        return result;
    }

    internal ValueTask<Term> CoreInsert407(ProcessContext context, Term identifier, Term objects)
        => CoreInsertPolicy414(
            context,
            identifier,
            objects,
            onlyNew: false
        );

    internal ValueTask<Term> CoreInsertNew414(ProcessContext context, Term identifier, Term objects)
        => CoreInsertPolicy414(
            context,
            identifier,
            objects,
            onlyNew: true
        );

    private async ValueTask<Term> CoreInsertPolicy414(
        ProcessContext context,
        Term identifier,
        Term objects,
        bool onlyNew
    )
    {
        RuntimeEtsFoundationTable admitted;
        RuntimeEtsCoreOptions407 metadata;
        lock (gate)
        {
            admitted = CoreResolve407(context, identifier, true);
            metadata = coreOptions407.GetValue(admitted, _ => throw new InvalidOperationException());
        }
        var values = CoreValidate407(objects, metadata.KeyPosition, context.Cancellation);
        if (values.Count == 0)
        {
            lock (gate)
            {
                if (!ReferenceEquals(CoreResolve407(context, identifier, true), admitted))
                    throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);

                return Term.A(ErlangBooleanAtoms.True);
            }
        }
        var staged = new List<RuntimeEtsStoredObject>(values.Count);
        var retained = new HashSet<RuntimeEtsStoredObject>(ReferenceEqualityComparer.Instance);
        var removed = new List<RuntimeEtsStoredObject>();
        bool committed = false;
        Task retirement = Task.CompletedTask;
        RuntimeEtsRetirement? preparedRetirement = null;
        try
        {
            foreach (var value in values) staged.Add(RuntimeEtsStoredObject.Capture(
                accounting,
                runtime,
                value,
                context.Cancellation
            ));
            lock (gate)
            {
                var table = CoreResolve407(context, identifier, true);
                if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                var existing = CoreRows420(table).ToArray();
                var oldValues = existing.Select(slot => CoreRead407(slot.Payload, context.Cancellation)).ToArray();
                var newValues = staged.Select(payload => CoreRead407(payload, context.Cancellation)).ToArray();
                var plan = EtsCorePlanner407.Plan(
                    oldValues,
                    newValues,
                    metadata.Kind,
                    metadata.KeyPosition,
                    onlyNew,
                    context.Cancellation
                );
                if (plan is null) return Term.A(ErlangBooleanAtoms.False);
                var next = new Dictionary<long, RuntimeEtsObjectSlot>();
                var order = new LinkedList<RuntimeEtsObjectSlot>();
                long last = table.LastObjectId;
                foreach (var selected in plan)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    var payload = selected.Incoming ? staged[selected.Index] : existing[selected.Index].Payload;
                    long id = selected.Incoming ? checked(++last) : existing[selected.Index].Id;
                    var slot = new RuntimeEtsObjectSlot(id, payload);
                    slot.Link = order.AddLast(slot);
                    next.Add(id, slot);
                    retained.Add(payload);
                }
                foreach (var old in existing) if (!retained.Contains(old.Payload)) removed.Add(old.Payload);
                context.Cancellation.ThrowIfCancellationRequested();
                var change420 = CoreStageIndex420(
                    table,
                    order,
                    removed.Concat(staged.Where(value => !retained.Contains(value))),
                    context.Cancellation
                );
                preparedRetirement = CorePrepareRetirement407(context.Self, change420.Retired);
                CorePublishIndex420(table, change420);
                table.LastObjectId = last;
                committed = true;
                retirement = CoreBeginRetirement407(preparedRetirement);
            }
            await retirement.ConfigureAwait(false);

            return Term.A(ErlangBooleanAtoms.True);
        }
        finally
        {
            if (!committed)
            {
                if (preparedRetirement is not null)
                {
                    preparedRetirement.Objects = null; // old rows still belong to the unchanged table
                    RetirementEnded(preparedRetirement, Array.Empty<Exception>());
                }
                foreach (var value in staged) value.Release();
            }
        }
    }

    internal Term CoreLookup407(ProcessContext context, Term identifier, Term key)
    {
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, false);
            var metadata = coreOptions407.GetValue(table, _ => throw new InvalidOperationException());
            var result = new List<Term>();
            if (CoreKeyGroup420(table, key, context.Cancellation) is { } group)
                foreach (var slot in group.Rows)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    result.Add(CoreRead407(slot.Payload, context.Cancellation));
                }

            return Cons.From(result);
        }
    }

    internal Term CoreMember407(ProcessContext context, Term identifier, Term key)
    {
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, false);
            var metadata = coreOptions407.GetValue(table, _ => throw new InvalidOperationException());
            if (CoreKeyGroup420(table, key, context.Cancellation) is { Deleted: false }) return Term.A(ErlangBooleanAtoms.True);

            return Term.A(ErlangBooleanAtoms.False);
        }
    }

    internal async ValueTask<Term> CoreDeleteKey407(ProcessContext context, Term identifier, Term key)
    {
        var removed = new List<RuntimeEtsStoredObject>();
        Task retirement;
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, true);
            var metadata = coreOptions407.GetValue(table, _ => throw new InvalidOperationException());
            var next = new Dictionary<long, RuntimeEtsObjectSlot>();
            var order = new LinkedList<RuntimeEtsObjectSlot>();
            foreach (var old in CoreRows420(table))
            {
                context.Cancellation.ThrowIfCancellationRequested();
                var value = CoreRead407(old.Payload, context.Cancellation);
                if (EtsCorePlanner407.SameKey(CoreKey407(value, metadata.KeyPosition), key, metadata.Kind)) removed.Add(old.Payload);
                else
                {
                    var slot = new RuntimeEtsObjectSlot(old.Id, old.Payload);
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
        await retirement.ConfigureAwait(false);

        return Term.A(ErlangBooleanAtoms.True);
    }

    internal async ValueTask<Term> CoreDeleteTable407(ProcessContext context, Term identifier)
    {
        Task retirement;
        lock (gate)
        {
            _ = CoreResolve407(context, identifier, true);
            retirement = DestroyTableAsync(context, identifier);
        }
        await retirement.ConfigureAwait(false); // actual tracked actor retirement, never dying-context cancellation

        return Term.A(ErlangBooleanAtoms.True);
    }
}
