namespace Erlang;

// Internal allocation/ownership foundation only. Object IDs are storage slots,
// not ETS keys, continuations, table types, options, or a guest API.
internal sealed partial class RuntimeEtsFoundation(ProcessRuntime runtime, object gate)
{
    private Dictionary<ReferenceResourceEntry, RuntimeEtsFoundationTable> tables = new(ReferenceEqualityComparer.Instance);
    private Dictionary<string, RuntimeEtsFoundationTable> names = new(StringComparer.Ordinal);
    private Dictionary<Pid, RuntimeEtsOwnerTables> owners = new();
    private LinkedList<RuntimeEtsOwnerTables> ownerOrder = new();
    private readonly RuntimeEtsArrayAccounting accounting = new();
    private bool closed;

    private void RequireActor(ProcessContext context)
    {
        ObjectDisposedException.ThrowIf(closed, RuntimeEtsFoundationNames.ClosedStorage);
        runtime.RequireEtsFoundationActor(context);
        context.Cancellation.ThrowIfCancellationRequested();
    }

    internal ResourceReferenceTerm CreateOwnerTable(
        ProcessContext context,
        Atom name,
        bool named,
        RuntimeEtsProtection protection
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        if (protection is not (RuntimeEtsProtection.Private or RuntimeEtsProtection.Protected or RuntimeEtsProtection.Public))
            throw new ArgumentOutOfRangeException(nameof(protection));
        lock (gate)
        {
            RequireActor(context);
            if (named && names.TryGetValue(name.Name, out var previous))
            {
                if (IsTableVisible419(previous))
                    throw new InvalidOperationException(RuntimeEtsFoundationNames.NameInUse);
                names.Remove(name.Name);
            }
            if (!owners.TryGetValue(context.Self, out var ownership))
            {
                ownership = new(context.Self);
                ownership.GlobalLink = ownerOrder.AddLast(ownership);
                owners.Add(context.Self, ownership);
            }
            var table = new RuntimeEtsFoundationTable(
                context.Self,
                name,
                named,
                protection,
                ownership,
                runtime.EtsCreationLane420(context)
            );
            var creationLink = new LinkedListNode<RuntimeEtsFoundationTable>(table);
            ResourceReferenceTerm handle = runtime.ReferenceResources.CreateEtsFoundation(table);
            table.Handle = handle;
            try
            {
                tables.Add(handle.Entry, table);
                table.OwnerLink = ownership.Tables.AddLast(table);
                if (named) names.Add(name.Name, table);
                creationLanes420[table.CreationLane420 - ProcessSchedulerNames.First].AddLast(creationLink);
                table.CreationLink420 = creationLink;

                return handle;
            }
            catch
            {
                creationLink.List?.Remove(creationLink);
                tables.Remove(handle.Entry);
                if (table.OwnerLink is { } ownerLink) ownership.Tables.Remove(ownerLink);
                if (named && names.TryGetValue(name.Name, out var current) && ReferenceEquals(current, table)) names.Remove(name.Name);
                handle.Entry.Close();
                throw;
            }
        }
    }

    private RuntimeEtsFoundationTable Resolve(Term identifier)
    {
        RuntimeEtsFoundationTable? table = null;
        if (identifier is Atom name)
            names.TryGetValue(name.Name, out table);
        else if (identifier is ReferenceTerm reference)
        {
            using var lease = runtime.ReferenceResources.AcquireEtsFoundation(reference);
            if (lease is not null)
            {
                var candidate = lease.Value;
                if (tables.TryGetValue(candidate.Handle.Entry, out var registered) && ReferenceEquals(candidate, registered)) table = candidate;
            }
        }
        if (table is null || !IsTableVisible419(table)) throw new InvalidOperationException(RuntimeEtsFoundationNames.InvalidTable);

        return table;
    }

    private void RequireAccess(ProcessContext context, RuntimeEtsFoundationTable table, bool write)
    {
        if (table.Owner.Equals(context.Self) || IsSuperUser420(context)) return;
        if (table.Protection == RuntimeEtsProtection.Private || write && table.Protection != RuntimeEtsProtection.Public)
            throw new InvalidOperationException(RuntimeEtsFoundationNames.AccessDenied);
    }

    internal long StoreTuple(ProcessContext context, Term identifier, TupleTerm value)
    {
        ArgumentNullException.ThrowIfNull(value);
        RuntimeEtsFoundationTable admitted;
        lock (gate)
        {
            RequireActor(context);
            admitted = Resolve(identifier);
            RequireAccess(context, admitted, true);
        }
        // Detach the composite and validate actual atomic domains outside runtime.gate.
        RuntimeEtsStoredObject staged = RuntimeEtsStoredObject.Capture(
            accounting,
            runtime,
            value,
            context.Cancellation
        );
        try
        {
            lock (gate)
            {
                RequireActor(context);
                var table = Resolve(identifier);
                if (!ReferenceEquals(table, admitted)) throw new InvalidOperationException(RuntimeEtsFoundationNames.InvalidTable);
                RequireAccess(context, table, true);
                if (table.Primary420 is not null) throw new InvalidOperationException(EtsIndexNames420.RawMutation);
                long id = checked(table.LastObjectId + 1);
                var slot = new RuntimeEtsObjectSlot(id, staged);
                slot.Link = table.ObjectOrder.AddLast(slot);
                try
                {
                    table.Objects.Add(id, slot);
                }
                catch
                {
                    table.ObjectOrder.Remove(slot.Link);
                    throw;
                }
                table.LastObjectId = id;

                return id;
            }
        }
        catch
        {
            staged.Release();
            throw;
        }
    }

    internal RuntimeEtsReadLease AcquireRead(ProcessContext context, Term identifier, long objectId)
    {
        lock (gate)
        {
            RequireActor(context);
            var table = Resolve(identifier);
            RequireAccess(context, table, false);
            if (!table.Objects.TryGetValue(objectId, out var value)) throw new InvalidOperationException(RuntimeEtsFoundationNames.MissingObject);

            return value.Payload.Acquire();
        }
    }

    internal Term ReadTuple(ProcessContext context, Term identifier, long objectId)
    {
        using RuntimeEtsReadLease lease = AcquireRead(context, identifier, objectId);

        return lease.Materialize(context.Cancellation);
    }

    internal bool RemoveTuple(ProcessContext context, Term identifier, long objectId)
    {
        RuntimeEtsObjectSlot? retired;
        lock (gate)
        {
            RequireActor(context);
            var table = Resolve(identifier);
            RequireAccess(context, table, true);
            if (table.Primary420 is not null) throw new InvalidOperationException(EtsIndexNames420.RawMutation);
            if (!table.Objects.Remove(objectId, out retired)) return false;
            table.ObjectOrder.Remove(retired.Link!);
        }
        retired.Payload.Release();

        return true;
    }

    internal void DestroyOwnerTable(ProcessContext context, Term identifier)
    {
        LinkedList<RuntimeEtsObjectSlot> retired;
        lock (gate)
        {
            RequireActor(context);
            var table = Resolve(identifier);
            if (!table.Owner.Equals(context.Self)) throw new InvalidOperationException(RuntimeEtsFoundationNames.NotOwner);
            retired = Detach(table);
        }
        foreach (var value in retired) value.Payload.Release();
    }

    private LinkedList<RuntimeEtsObjectSlot> Detach(RuntimeEtsFoundationTable table)
    {
        var retired = table.ObjectOrder;
        var emptyOrder = new LinkedList<RuntimeEtsObjectSlot>();
        var emptyObjects = new Dictionary<long, RuntimeEtsObjectSlot>();
        // Allocate optional gift node before detaching table/roots.
        LinkedListNode<RuntimeEtsObjectSlot>? heirNode = table.Heir419?.Data is { } data
            ? new(new RuntimeEtsObjectSlot(0, data)) : null;
        CoreRetireTableFixations420(table); // prepare allocations before changing gift/physical roots
        if (heirNode is not null) retired.AddLast(heirNode);
        table.Heir419 = null;
        table.Primary420 = null;
        table.ObjectOrder = emptyOrder;
        table.Objects = emptyObjects;
        table.Retired = true;
        if (table.CreationLink420 is { } creation) creation.List?.Remove(creation);
        table.CreationLink420 = null;
        tables.Remove(table.Handle.Entry);
        if (table.Named && names.TryGetValue(table.Name.Name, out var current) && ReferenceEquals(current, table)) names.Remove(table.Name.Name);
        table.Handle.Entry.Close();
        if (table.OwnerLink is { } link)
        {
            table.Ownership.Tables.Remove(link);
            table.OwnerLink = null;
        }

        return retired;
    }

    internal (int Tables, int Names, int Objects, long Payloads, long OwnedArrayPayloadBytes, bool Closed) Snapshot()
    {
        lock (gate)
        {
            var memory = accounting.Snapshot();

            return (tables.Count, names.Count, tables.Values.Sum(table => table.Objects.Count), memory.Payloads, memory.ArrayPayloadBytes, closed);
        }
    }
}
