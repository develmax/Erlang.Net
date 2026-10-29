namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly object ownedTermOwnersGate506 = new();
    private readonly Dictionary<ProcessContext, ProcessTermHeap506> ownedTermOwners506 = new();
    private bool ownedTermOwnersClosing506;

    private void RegisterOwnedTermContext506(ProcessContext context)
    {
        if (!OwnedTermsPilot506)
            return;
        lock (ownedTermOwnersGate506)
        {
            ObjectDisposedException.ThrowIf(ownedTermOwnersClosing506, this);
            ownedTermOwners506.Add(context, context.TermHeap506);
        }
    }

    internal void RegisterGenuineVmContext683(ProcessContext context)
    {
        if (!ReferenceEquals(context.Runtime, this) || !RuntimeActorExecutionTurn.IsOwnedBy(context)
            || !context.OwnedExecutionEnabled683 || OwnedTermsPilot506)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        lock (ownedTermOwnersGate506)
        {
            // Same gate as the real closing snapshot. A losing entrant creates
            // no heap frame; an admitted heap is in the snapshot before use.
            ObjectDisposedException.ThrowIf(ownedTermOwnersClosing506 || Volatile.Read(ref context.Terminated) || IsHaltRequested, this);
            context.Cancellation.ThrowIfCancellationRequested();
            ownedTermOwners506.TryAdd(context, context.TermHeap506);
        }
    }

    internal void TrackOwnedTermRetirement506(ProcessContext context, Task retirement)
    {
        if (!context.OwnedExecutionEnabled683)
            return;
        // No process/heap gate is held during continuation registration.
        if (ExecutionContext.IsFlowSuppressed())
            _ = RemoveAfterDrain506(context, retirement);
        else
        {
            using var suppressed = ExecutionContext.SuppressFlow();
            _ = RemoveAfterDrain506(context, retirement);
        }
    }

    private async Task RemoveAfterDrain506(ProcessContext context, Task retirement)
    {
        await retirement.ConfigureAwait(false);
        lock (ownedTermOwnersGate506)
            ownedTermOwners506.Remove(context);
    }

    private Task DrainOwnedTermContexts506()
    {
        KeyValuePair<ProcessContext, ProcessTermHeap506>[] owners;
        lock (ownedTermOwnersGate506)
        {
            ownedTermOwnersClosing506 = true;
            owners = ownedTermOwners506.ToArray();
        }
        // Close dictionary roots, but retain every admitted invocation/borrow
        // until its actual operation ends. Never wait on logical process DOWN.
        foreach (var owner in owners)
            owner.Key.BeginOwnedTermRetirement506();

        return Task.WhenAll(owners.Select(owner => owner.Key.OwnedTermRetirement506));
    }

    private void RequireOwnedTermShutdownCaller506()
    {
        if (OwnedTermInvocation506.HasActiveFor506(this))
            throw new InvalidOperationException(ProcessTerm506Names.SelfShutdown);
    }
}
