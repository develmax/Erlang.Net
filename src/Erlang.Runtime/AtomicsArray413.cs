using System.Runtime.InteropServices;
namespace Erlang;

internal sealed unsafe partial class AtomicsArray413
{
    private readonly object gate = new();
    private nint memory;
    private int active;
    private bool closed;
    private bool freed;
    private Exception? closeFailure;
    internal ulong AllocatedBytes { get; }
    private static nint DestructorAddress => (nint)(delegate* unmanaged<nint, int>)&Destroy;

    internal AtomicsArray413(ulong length, bool signed)
    {
        if (IntPtr.Size != sizeof(ulong)) throw new PlatformNotSupportedException(AtomicsNames413.Architecture);
        // Source rejects counts exceeding UWord/atomic size. Also reject overflow of our actual owned region.
        if (length > (ulong.MaxValue - (ulong)sizeof(AtomicsMagicHeader413)) / sizeof(long)
            || length > ((ulong)nint.MaxValue - (ulong)sizeof(AtomicsMagicHeader413)) / sizeof(long))
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        AllocatedBytes = checked((ulong)sizeof(AtomicsMagicHeader413) + length * sizeof(long));
        void* pointer = NativeMemory.AllocZeroed((nuint)AllocatedBytes);
        if (pointer == null) throw new OutOfMemoryException();
        memory = (nint)pointer;
        var header = (AtomicsMagicHeader413*)pointer;
        header->Destructor = DestructorAddress;
        header->AllocationKind = AtomicsNames413.AllocationKind;
        header->IsSigned = signed ? 1 : 0;
        header->Length = length;
    }

    internal void BindIdentity(IReadOnlyList<uint> words)
    {
        if (words.Count != AtomicsNames413.ReferenceWords) throw new InvalidOperationException(AtomicsNames413.Identity);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            var header = (AtomicsMagicHeader413*)memory;
            header->Reference0 = words[0];
            header->Reference1 = words[1];
            header->Reference2 = words[2];
        }
    }

    internal AtomicsArrayLease413? Acquire()
    {
        lock (gate)
        {
            if (closed) return null;
            var header = (AtomicsMagicHeader413*)memory;
            if (header->Destructor != DestructorAddress || header->AllocationKind != AtomicsNames413.AllocationKind)
                throw new InvalidOperationException(AtomicsNames413.Destructor);
            var lease = new AtomicsArrayLease413(this);
            active = checked(active + 1);

            return lease;
        }
    }

    internal void Release()
    {
        lock (gate)
        {
            if (--active < 0) throw new InvalidOperationException(AtomicsNames413.Underflow);
            if (active == 0) Monitor.PulseAll(gate);
        }
    }

    // Only an actual array lease may use this pointer. Close waits those leases before destroying it.
    private AtomicsMagicHeader413* Header => (AtomicsMagicHeader413*)memory;
    internal bool Signed => Header->IsSigned != 0;
    internal ulong Length => Header->Length;

    internal ref long Cell(ulong index) => ref *(long*)((byte*)memory + sizeof(AtomicsMagicHeader413) + checked((nint)(index * sizeof(long))));

    internal void Close()
    {
        nint retired;
        lock (gate)
        {
            if (closed)
            {
                while (!freed) Monitor.Wait(gate);
                if (closeFailure is not null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(closeFailure).Throw();

                return;
            }
            closed = true;
            while (active != 0) Monitor.Wait(gate);
            retired = memory;
            memory = 0;
        }
        Exception? failure = null;
        try
        {
            if (retired != 0)
            {
                var destructor = (delegate* unmanaged<nint, int>)((AtomicsMagicHeader413*)retired)->Destructor;
                try
                {
                    if (destructor(retired) != 1) throw new InvalidOperationException(AtomicsNames413.Destructor);
                }
                finally
                {
                    ReleaseNativeWindow434((void*)retired);
                }
            }
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            lock (gate)
            {
                closeFailure = failure;
                freed = true;
                Monitor.PulseAll(gate);
            }
        }
    }

    [UnmanagedCallersOnly]
    private static int Destroy(nint unused) => 1;
}
