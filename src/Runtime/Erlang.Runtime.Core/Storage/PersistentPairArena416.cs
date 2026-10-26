using System.Runtime.InteropServices;

namespace Erlang;

// The native block is the authoritative payload, not an allocation-size shadow.
internal sealed unsafe class PersistentPairArena416 : SafeHandle
{
    private readonly object gate = new();
    private PersistentArenaRoots416? roots;
    private bool retired;
    private readonly PersistentArenaAccounting416 accounting;
    private long allocated;

    private PersistentPairArena416(PersistentArenaImage416 image, PersistentArenaAccounting416 accounting) : base(0, true)
    {
        this.accounting = accounting;
        roots = image.Roots;
        RequestedBytes = image.Bytes.LongLength;
        SetHandle(accounting.Allocate(PersistentAllocationKind416.Pair, RequestedBytes));
        try
        {
            Marshal.Copy(
                image.Bytes,
                0,
                handle,
                image.Bytes.Length
            );
            allocated = RequestedBytes;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public override bool IsInvalid => handle == 0;
    internal long RequestedBytes { get; }
    internal long AllocatedBytes => Interlocked.Read(ref allocated);

    internal static PersistentPairArena416 Capture(
        ProcessRuntime owner,
        Term key,
        Term value,
        CancellationToken cancellation,
        PersistentArenaAccounting416? accounting = null
    )
    {
        PersistentArenaImage416 image = PersistentArenaCodec416.Encode(owner, Term.Tuple(key, value), cancellation);
        try
        {
            cancellation.ThrowIfCancellationRequested();

            return new(image, accounting ?? new());
        }
        catch
        {
            image.Roots.Dispose();
            throw;
        }
    }

    internal PersistentArenaRead416 AcquireRead()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retired, this);
            bool acquired = false;
            DangerousAddRef(ref acquired);
            try
            {
                return new(
                    this,
                    handle,
                    checked((int)RequestedBytes),
                    roots!
                );
            }
            catch
            {
                if (acquired) DangerousRelease();
                throw;
            }
        }
    }

    internal void Retire()
    {
        lock (gate)
        {
            if (retired) return;
            retired = true;
            Dispose();
        }
    }

    protected override bool ReleaseHandle()
    {
        // SafeHandle may call this on a finalizer. No managed exception may escape.
        try
        {
            Interlocked.Exchange(ref roots, null)?.Dispose();
        }
        catch (Exception failure)
        {
            accounting.ReportReleaseFailure(failure);
        }
        try
        {
            accounting.Free(PersistentAllocationKind416.Pair, handle, RequestedBytes);
        }
        catch (Exception failure)
        {
            accounting.ReportReleaseFailure(failure);
        }
        Interlocked.Exchange(ref allocated, 0);

        return true;
    }
}
