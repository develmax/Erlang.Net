namespace Erlang;

// Legacy synchronous Host waiters may hold M on their disposing thread. Their
// queued custody must be invalidatable by VM close until the original callback
// atomically upgrades to an Operation; it must not hold an Operation while
// blocked in uncancellable CodeModification.Enter.
internal sealed class PreparedLoaderWaiting807(
    ReferenceResourceEntry entry,
    PreparedLoaderOwner807 owner,
    PreparedLoaderHost807 host
) : IDisposable
{
    internal ReferenceResourceEntry Entry807 { get; } = entry;
    internal PreparedLoaderOwner807 Owner807 { get; } = owner;
    private PreparedLoaderHost807? held = host;
    internal bool IsHeld807 => Volatile.Read(ref held)?.Owns807(Owner807) == true;

    public void Dispose() => Interlocked.Exchange(ref held, null)?.Dispose();
}
