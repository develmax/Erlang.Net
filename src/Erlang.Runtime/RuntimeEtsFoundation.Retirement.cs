namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private readonly HashSet<RuntimeEtsRetirement> retirements = new();
    private readonly List<Exception> retirementFailures = new();
    private readonly Dictionary<Pid, HashSet<RuntimeEtsRetirement>> actorRetirements = new();
    private long retirementYields;
    internal Task ShutdownRetirement { get; private set; } = Task.CompletedTask;

    internal bool HasPendingRetirements
    {
        get
        {
            lock (gate) return retirements.Count != 0;
        }
    }

    internal long RetirementYields => Interlocked.Read(ref retirementYields);

    internal void RetirementYielded() => Interlocked.Increment(ref retirementYields);

    internal Task ProcessTerminated(Pid owner)
    {
        lock (gate)
        {
            if (closed)
                return JoinActorRetirements(owner, ShutdownRetirement);
            CoreRetireProcessFixations420(owner);
            if (!owners.Remove(owner, out var group))
                return JoinActorRetirements(owner, Task.CompletedTask);
            group.Retired = true;
            if (group.GlobalLink is { } link) ownerOrder.Remove(link);
            group.GlobalLink = null;
            if (group.Tables.Count == 0)
                return JoinActorRetirements(owner, Task.CompletedTask);
            var selected = new LinkedList<RuntimeEtsOwnerTables>();
            selected.AddLast(group);

            return JoinActorRetirements(owner, StartRetirement(selected, owner));
        }
    }

    internal void Close()
    {
        lock (gate)
        {
            if (closed) return;
            closed = true;
            var selected = ownerOrder;
            ownerOrder = new();
            owners = new();
            tables = new(ReferenceEqualityComparer.Instance);
            names = new(StringComparer.Ordinal);
            if (selected.Count != 0) ShutdownRetirement = StartRetirement(selected);
        }
    }

    private Task JoinActorRetirements(Pid actor, Task additional)
    {
        if (!actorRetirements.TryGetValue(actor, out var pending) || pending.Count == 0)
            return additional;

        return Task.WhenAll(pending.Select(item => item.Execution).Append(additional).Distinct());
    }

    private Task StartRetirement(LinkedList<RuntimeEtsOwnerTables> selected, Pid? actor = null)
    {
        var retirement = new RuntimeEtsRetirement(this, selected, actor);
        retirements.Add(retirement);
        if (actor is not null)
        {
            if (!actorRetirements.TryGetValue(actor, out var pending))
                actorRetirements.Add(actor, pending = new());
            pending.Add(retirement);
        }
        retirement.Execution = retirement.ExecuteAsync();

        return retirement.Execution;
    }

    internal bool RetireNext(RuntimeEtsRetirement retirement, out RuntimeEtsStoredObject? value, out RuntimeEtsHeirTransfer419? transfer)
    {
        value = null;
        transfer = null;
        if (CoreRetireNextFixation420(retirement)) return true;
        if (retirement.Objects?.First is { } objectNode)
        {
            retirement.Objects.RemoveFirst();
            value = objectNode.Value.Payload;

            return true;
        }
        retirement.Objects = null;
        lock (gate)
        {
            if (retirement.Groups.First is not { } groupNode)
                return false;
            var group = groupNode.Value;
            group.Retired = true;
            if (group.Tables.First is { } tableNode)
            {
                transfer=PrepareHeirTransfer419(tableNode.Value,group);
                if(transfer is null)retirement.Objects = Detach(tableNode.Value);
            }
            else
                retirement.Groups.RemoveFirst();

            return true;
        }
    }

    internal void RetirementEnded(RuntimeEtsRetirement retirement, IReadOnlyList<Exception> failures)
    {
        lock (gate)
        {
            try
            {
                retirementFailures.AddRange(failures);
            }
            finally
            {
                retirements.Remove(retirement);
                if (retirement.Actor is { } actor && actorRetirements.TryGetValue(actor, out var pending))
                {
                    pending.Remove(retirement);
                    if (pending.Count == 0) actorRetirements.Remove(actor);
                }
            }
        }
    }

    internal Task DestroyTableAsync(ProcessContext context, Term identifier)
    {
        lock (gate)
        {
            RequireActor(context);
            var table = Resolve(identifier);
            RequireAccess(context, table, true);
            var deletion = new RuntimeEtsOwnerTables(context.Self) { Retired = true };
            var selected = new LinkedList<RuntimeEtsOwnerTables>();
            selected.AddLast(deletion);
            var link = deletion.Tables.AddLast(table);
            table.Ownership.Tables.Remove(table.OwnerLink!);
            table.Ownership = deletion;
            table.OwnerLink = link;
            table.Retired = true;
            table.Handle.Entry.Close();

            return StartRetirement(selected, context.Self);
        }
    }

    internal async Task DrainAsync()
    {
        while (true)
        {
            Task[] current;
            lock (gate)
            {
                current = retirements.Select(item => item.Execution).Concat(allRequests420.Select(request=>request.Completion)).ToArray();
                if (current.Length == 0)
                {
                    if (retirementFailures.Count != 0)
                        throw new AggregateException(retirementFailures);

                    return;
                }
            }
            try
            {
                await Task.WhenAll(current).ConfigureAwait(false);
            }
            catch
            {
                // Each failure is retained before removal. Join independent owners too.
            }
        }
    }
}
