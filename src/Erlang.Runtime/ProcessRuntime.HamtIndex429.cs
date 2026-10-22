namespace Erlang;

public sealed partial class ProcessRuntime
{
    private HamtIndexOwner429? hamtIndex429;
    private bool hamtAdmissionClosed429;

    internal HamtIndexRead429 AcquireHamtIndex429(MapTerm map)
    {
        HamtIndexOwner429 owner;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(hamtAdmissionClosed429 || disposed || IsHaltRequested, this);
            owner = hamtIndex429 ??= new(this);
        }

        return owner.Acquire(map);
    }

    internal void RequireHamtNode429(HamtBitmapTerm429 node)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(hamtAdmissionClosed429 || disposed || IsHaltRequested, this);
            if (hamtIndex429 is null || !ReferenceEquals(hamtIndex429.Domain, node.Domain))
                throw new ArgumentException(HamtIndexNames429.Foreign);
        }
    }

    private Task CloseHamtIndex429()
    {
        hamtAdmissionClosed429 = true;

        return hamtIndex429?.Close() ?? Task.CompletedTask;
    }

    private Task DrainHamtIndex429()
    {
        lock (gate) return CloseHamtIndex429();
    }

    private Task HamtProcessRetirement429(ProcessContext context, Task previous)
        => !disposed && !context.Self.Equals(InitializationPid) ? previous : Task.WhenAll(previous, CloseHamtIndex429());
}
