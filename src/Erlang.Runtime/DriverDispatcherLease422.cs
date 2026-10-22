namespace Erlang;

// This is a genuine dispatcher owner reservation, not a scheduler-thread flag.
// Retiring rejects new guest work as before, but exact live owner may finish its
// already-owned native tasks. Completion remains pending until real release.
internal sealed class DriverDispatcherLease422 : IDisposable
{
    private RuntimeActorDispatcher? owner;

    internal DriverDispatcherLease422(RuntimeActorDispatcher owner) => this.owner = owner;

    internal bool IsOwnedBy(RuntimeActorDispatcher expected) => ReferenceEquals(Volatile.Read(ref owner), expected);

    public void Dispose()
    {
        RuntimeActorDispatcher? selected = Interlocked.Exchange(ref owner, null);
        selected?.ReleaseDriverOwner422();
    }
}
