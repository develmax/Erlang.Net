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

    internal void TrackOwnedTermRetirement506(ProcessContext context, Task retirement)
    {
        if (!OwnedTermsPilot506)
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
        if (!OwnedTermsPilot506)
            return Task.CompletedTask;
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
        if (OwnedTermInvocation506.Active is { IsDisposed: false } invocation
            && ReferenceEquals(invocation.Runtime, this))
            throw new InvalidOperationException(ProcessTerm506Names.SelfShutdown);
    }
}
