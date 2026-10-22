namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private bool IsTableVisible419(RuntimeEtsFoundationTable table)
     => !table.Retired && (!table.Ownership.Retired || !closed && table.Heir419 is { } heir && !table.Owner.Equals(heir.Pid) && runtime.ReadEtsHeirProcess419(heir) is not null);

    private RuntimeEtsHeirTransfer419? PrepareHeirTransfer419(RuntimeEtsFoundationTable table, RuntimeEtsOwnerTables group)
    {
        if (closed || table.Retired || table.Heir419 is not { } heir || table.Owner.Equals(heir.Pid) || runtime.ReadEtsHeirProcess419(heir) is null) return null;
        RuntimeEtsReadLease? read = heir.Data?.Acquire();
        try
        {
            return new(
                table,
                group,
                heir,
                read
            );
        }
        catch
        {
            read?.Dispose();
            throw;
        }
    }

    // All allocations precede moving the table; caller holds actual runtime gate.
    private void MoveOwner419(RuntimeEtsFoundationTable table, ProcessContext target)
    {
        RuntimeEtsOwnerTables group;
        LinkedListNode<RuntimeEtsOwnerTables>? global = null;
        if (!owners.TryGetValue(target.Self, out group!))
        {
            group = new(target.Self);
            global = new(group);
            owners.EnsureCapacity(checked(owners.Count + 1));
        }
        if (global is not null)
        {
            owners.Add(target.Self, group);
            ownerOrder.AddLast(global);
            group.GlobalLink = global;
        }
        var link = table.OwnerLink!;
        table.Ownership.Tables.Remove(link);
        group.Tables.AddLast(link);
        table.Ownership = group;
        table.Owner = target.Self;
    }

    internal void CompleteHeirTransfer419(RuntimeEtsRetirement retirement, RuntimeEtsHeirTransfer419 work)
    {
        try
        {
            Term? gift = work.Read is null ? null : ((TupleTerm)work.Read.Materialize(CancellationToken.None)).Items[0];
            while (true)
            {
                ProcessContext target;
                Atom name;
                Term identifier;
                lock (gate)
                {
                    var table = work.Table;
                    if (table.Retired || !ReferenceEquals(table.Ownership, work.Group)) return;
                    var alive = !closed && ReferenceEquals(table.Heir419, work.Heir) ? runtime.ReadEtsHeirProcess419(work.Heir) : null;
                    if (alive is null)
                    {
                        retirement.Objects = Detach(table);

                        return;
                    }
                    target = alive;
                    name = table.Name;
                    identifier = table.Named ? name : table.Handle;
                }
                // All observer callbacks, sharing pins and queue-node/signal allocations precede publication.
                Mailbox.EtsAdmission419? admission = gift is null ? null : target.Mailbox.PrepareEtsTransfer419(target,
                 Term.Tuple(
                     Term.A(EtsHeirNames419.Transfer),
                     identifier,
                     work.Group.Owner,
                     gift
                 ), work.Group.Owner);
                lock (gate)
                {
                    var table = work.Table;
                    if (table.Retired || !ReferenceEquals(table.Ownership, work.Group)) return;
                    if (closed || !ReferenceEquals(table.Heir419, work.Heir) || !ReferenceEquals(runtime.ReadEtsHeirProcess419(work.Heir), target))
                    {
                        retirement.Objects = Detach(table);

                        return;
                    }
                    if (table.Named && !table.Name.Equals(name)) continue;
                    admission?.Validate(target.Mailbox);
                    MoveOwner419(table, target);
                    admission?.Track(target);
                    // Exact receiver cannot run an owner BIF/terminate across this shared runtime gate.
                    admission?.Commit(target.Mailbox);
                }
                admission?.Notify();

                return;
            }
        }
        catch
        {
            lock (gate)
                if (!work.Table.Retired && ReferenceEquals(work.Table.Ownership, work.Group)) retirement.Objects = Detach(work.Table);
            throw;
        }
        finally
        {
            work.Dispose();
        }
    }
}
