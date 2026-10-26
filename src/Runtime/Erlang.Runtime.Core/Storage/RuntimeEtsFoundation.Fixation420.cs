namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private readonly Dictionary<Pid, LinkedList<RuntimeEtsFixation420>> fixingProcesses420 = new();

    internal async ValueTask<Term> CoreSafeFix420(ProcessContext context, Term identifier, Term requested)
    {
        Task retirement = Task.CompletedTask;
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, false);
            if (requested is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } value) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            if (value.Name == ErlangBooleanAtoms.True)
            {
                var started = table.FixedCount420 == 0 ? ErlangClockState.MonotonicNanoseconds : table.FixedAtMonotonic420;
                var offset = table.FixedCount420 == 0 ? ErlangClockState.OffsetNanoseconds : table.FixedOffset420;
                long count = checked(table.FixedCount420 + 1);
                if (table.Fixations420.TryGetValue(context.Self, out var previous))
                {
                    previous.Count = checked(previous.Count + 1);
                    table.FixedCount420 = count;
                }
                else
                {
                    var fixation = new RuntimeEtsFixation420(context.Self, table);
                    var tableNode = new LinkedListNode<RuntimeEtsFixation420>(fixation);
                    var processNode = new LinkedListNode<RuntimeEtsFixation420>(fixation);
                    bool fresh = !fixingProcesses420.TryGetValue(context.Self, out var list);
                    list ??= new();
                    table.Fixations420.EnsureCapacity(checked(table.Fixations420.Count + 1));
                    if (fresh) fixingProcesses420.EnsureCapacity(checked(fixingProcesses420.Count + 1));
                    if (fresh) fixingProcesses420.Add(context.Self, list);
                    table.Fixations420.Add(context.Self, fixation);
                    fixation.TableLink = tableNode;
                    fixation.ProcessLink = processNode;
                    table.FixedOrder420.AddLast(tableNode);
                    list.AddLast(processNode);
                    table.FixedCount420 = count;
                }
                table.FixedAtMonotonic420 = started;
                table.FixedOffset420 = offset;
            }
            else if (table.Fixations420.TryGetValue(context.Self, out var fixation))
            {
                EtsIndexChange420? change = table.FixedCount420 == 1 ? CoreStageIndex420(
                    table,
                    CoreRows420(table),
                    Array.Empty<RuntimeEtsStoredObject>(),
                    context.Cancellation,
                    false
                ) : null;
                var prepared = change is null ? null : CorePrepareRetirement407(context.Self, change.Retired);
                if (fixation.Count == 1) CoreRemoveFixation420(fixation);
                else
                {
                    fixation.Count--;
                    table.FixedCount420--;
                }
                if (change is not null) CorePublishIndex420(table, change);
                retirement = CoreBeginRetirement407(prepared);
            }
        }
        await retirement.ConfigureAwait(false);

        return Term.A(ErlangBooleanAtoms.True);
    }

    private void CoreRemoveFixation420(RuntimeEtsFixation420 fixation)
    {
        if (!fixation.Active) return;
        fixation.Active = false;
        var table = fixation.Table;
        if (ReferenceEquals(fixation.TableLink?.List, table.FixedOrder420)) table.FixedCount420 -= fixation.Count;
        if (fixation.TableLink is { } tableNode) tableNode.List?.Remove(tableNode);
        fixation.TableLink = null;
        var processNode = fixation.ProcessLink;
        var processList = processNode?.List;
        if (processNode is not null) processList?.Remove(processNode);
        fixation.ProcessLink = null;
        if (processList is { Count: 0 } && fixingProcesses420.TryGetValue(fixation.Process, out var registered) && ReferenceEquals(registered, processList)) fixingProcesses420.Remove(fixation.Process);
        table.Fixations420.Remove(fixation.Process);
    }

    private void CoreRetireProcessFixations420(Pid process)
    {
        if (!fixingProcesses420.TryGetValue(process, out var rows) || rows.Count == 0) return;
        var retirement = CorePrepareFixationJob420(rows, process);
        fixingProcesses420.Remove(process);
        retirement.Execution = retirement.ExecuteAsync();
    }

    private void CoreRetireTableFixations420(RuntimeEtsFoundationTable table)
    {
        if (table.FixedOrder420.Count == 0) return;
        var empty = new LinkedList<RuntimeEtsFixation420>();
        var retirement = CorePrepareFixationJob420(table.FixedOrder420, null);
        table.FixedOrder420 = empty;
        table.FixedCount420 = 0;
        retirement.Execution = retirement.ExecuteAsync();
    }

    private RuntimeEtsRetirement CorePrepareFixationJob420(LinkedList<RuntimeEtsFixation420> rows, Pid? actor)
    {
        var retirement = new RuntimeEtsRetirement(this, new(), actor) { Fixations420 = rows };
        retirements.EnsureCapacity(checked(retirements.Count + 1));
        HashSet<RuntimeEtsRetirement>? pending = null;
        if (actor is not null)
        {
            if (!actorRetirements.TryGetValue(actor, out pending))
            {
                pending = new();
                pending.EnsureCapacity(1);
                actorRetirements.Add(actor, pending);
            }
            else pending.EnsureCapacity(checked(pending.Count + 1));
        }
        retirements.Add(retirement);
        pending?.Add(retirement);

        return retirement;
    }

    private bool CoreRetireNextFixation420(RuntimeEtsRetirement retirement)
    {
        if (retirement.Objects is { Count: > 0 }) return false;
        lock (gate)
        {
            if (retirement.Fixations420?.First is not { } first) return false;
            var fixation = first.Value;
            var table = fixation.Table;
            EtsIndexChange420? change = fixation.Active && !closed && !table.Retired && ReferenceEquals(fixation.TableLink?.List, table.FixedOrder420) && table.FixedCount420 == fixation.Count
             ? CoreStageIndex420(
                 table,
                 CoreRows420(table),
                 Array.Empty<RuntimeEtsStoredObject>(),
                 CancellationToken.None,
                 false
             ) : null;
            LinkedList<RuntimeEtsObjectSlot>? purged = null;
            if (change is not null)
            {
                purged = new();
                foreach (var payload in change.Retired) purged.AddLast(new RuntimeEtsObjectSlot(0, payload));
            }
            // Remove may splice the same node from this job and a concurrent table job.
            CoreRemoveFixation420(fixation);
            if (ReferenceEquals(first.List, retirement.Fixations420)) retirement.Fixations420.Remove(first);
            if (change is not null)
            {
                CorePublishIndex420(table, change);
                retirement.Objects = purged;
            }

            return true;
        }
    }

    internal Term CoreSlot420(ProcessContext context, Term identifier, Term requested)
    {
        lock (gate)
        {
            var table = CoreResolve407(context, identifier, false);
            if (requested is not Integer position || position.Value < 0 || position.Value > RuntimeEtsCoreNames407.MaximumSmall64) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);
            long extent = index.Kind == EtsCoreTableKind407.OrderedSet ? index.Ordered.LongLength : index.BucketCount;
            if (position.Value == extent) return Term.A(EtsTraversalNames416.End);
            if (position.Value > extent) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            IEnumerable<RuntimeEtsObjectSlot> rows = index.Kind == EtsCoreTableKind407.OrderedSet ? index.Ordered[(int)position.Value].Rows : index.Buckets[(int)position.Value].SelectMany(group => group.Rows);

            return Cons.From(rows.Select(slot => CoreRead407(slot.Payload, context.Cancellation)));
        }
    }
}
