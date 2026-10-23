using System.Runtime.InteropServices;
namespace Erlang;

internal sealed unsafe partial class CountersArray421
{
    private readonly object gate = new();
    private nint memory;
    private int active;
    private bool closed;
    private bool freed;
    private Exception? closeFailure;
    internal ulong AllocatedBytes { get; }
    private static nint DestructorAddress => (nint)(delegate* unmanaged<nint, int>)&Destroy;

    private readonly int lanes;

    internal CountersArray421(ulong length, int lanes)
    {
        if (IntPtr.Size != sizeof(ulong)) throw new PlatformNotSupportedException(CountersNames421.Architecture);
        if (lanes < 1) throw new ArgumentOutOfRangeException(nameof(lanes));
        ulong copies = checked((ulong)lanes + 1);
        if (length > ulong.MaxValue / (sizeof(long) * 2 * copies))
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        ulong lines = checked(copies * ((length + CountersNames421.CellsPerLine - 1) / CountersNames421.CellsPerLine));
        ulong cellsBytes = checked(lines * CountersNames421.CacheLineBytes);
        if (cellsBytes > (ulong)nint.MaxValue - (ulong)sizeof(CountersMagicHeader421))
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        AllocatedBytes = checked((ulong)sizeof(CountersMagicHeader421) + cellsBytes);
        void* pointer = NativeMemory.AllocZeroed((nuint)AllocatedBytes);
        if (pointer == null) throw new OutOfMemoryException();
        this.lanes = lanes;
        memory = (nint)pointer;
        var header = (CountersMagicHeader421*)pointer;
        header->Destructor = DestructorAddress;
        header->AllocationKind = CountersNames421.AllocationKind;
        header->Length = length;
    }

    internal void BindIdentity(IReadOnlyList<uint> words)
    {
        if (words.Count != CountersNames421.ReferenceWords) throw new InvalidOperationException(CountersNames421.Identity);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, this);
            var header = (CountersMagicHeader421*)memory;
            header->Reference0 = words[0];
            header->Reference1 = words[1];
            header->Reference2 = words[2];
        }
    }

    internal CountersArrayLease421? Acquire()
    {
        lock (gate)
        {
            if (closed) return null;
            var header = (CountersMagicHeader421*)memory;
            if (header->Destructor != DestructorAddress || header->AllocationKind != CountersNames421.AllocationKind)
                throw new InvalidOperationException(CountersNames421.Destructor);
            var lease = new CountersArrayLease421(this);
            active = checked(active + 1);

            return lease;
        }
    }

    internal void Release()
    {
        lock (gate)
        {
            if (--active < 0) throw new InvalidOperationException(CountersNames421.Underflow);
            if (active == 0) Monitor.PulseAll(gate);
        }
    }

    // Only an actual array lease may use this pointer. Close waits those leases before destroying it.
    private CountersMagicHeader421* Header => (CountersMagicHeader421*)memory;
    internal ulong Length => Header->Length;

    internal int Lanes => lanes;

    internal ref long Cell(ulong index, int lane)
    {
        if ((uint)lane > (uint)lanes) throw new ArgumentOutOfRangeException(nameof(lane));
        ulong line = checked((index / CountersNames421.CellsPerLine) * ((ulong)lanes + 1) + (uint)lane);
        ulong offset = checked(line * CountersNames421.CacheLineBytes + (index % CountersNames421.CellsPerLine) * sizeof(long));

        return ref *(long*)((byte*)memory + sizeof(CountersMagicHeader421) + checked((nint)offset));
    }

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
                var destructor = (delegate* unmanaged<nint, int>)((CountersMagicHeader421*)retired)->Destructor;
                try
                {
                    if (destructor(retired) != 1) throw new InvalidOperationException(CountersNames421.Destructor);
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
