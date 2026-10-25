namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    // Prepare/register before the guarded pointer swap. Execute only after it.
    // No fabricated table/resource/context; Objects owns actual retired row roots.
    private RuntimeEtsRetirement? CorePrepareRetirement407(Pid actor, IEnumerable<RuntimeEtsStoredObject> payloads)
    {
        var rows = new LinkedList<RuntimeEtsObjectSlot>();
        foreach (var payload in payloads) rows.AddLast(new RuntimeEtsObjectSlot(0, payload));
        if (rows.Count == 0) return null;
        var retirement = new RuntimeEtsRetirement(this, new(), actor) { Objects = rows };
        retirements.EnsureCapacity(checked(retirements.Count + 1));
        if (!actorRetirements.TryGetValue(actor, out var pending))
        {
            pending = new();
            pending.EnsureCapacity(1);
            actorRetirements.Add(actor, pending);
        }
        else pending.EnsureCapacity(checked(pending.Count + 1));
        retirements.Add(retirement);
        pending.Add(retirement);

        return retirement;
    }

    private static Task CoreBeginRetirement407(RuntimeEtsRetirement? retirement)
    {
        if (retirement is null) return Task.CompletedTask;
        retirement.Execution = retirement.ExecuteAsync();

        return retirement.Execution;
    }
}
