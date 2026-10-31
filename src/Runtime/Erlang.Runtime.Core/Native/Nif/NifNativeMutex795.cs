namespace Erlang;

// Real synchronization state, addressed by an opaque native allocation. A CLR
// monitor protects the platform lock state; the monitor itself is not exposed.
internal sealed class NifNativeMutex795(NifNativeAllocationOwner owner, nint pointer, nint name)
{
    private readonly object gate = new();
    private int holder, waiters, recursion;
    private bool destroyed;
    internal NifNativeAllocationOwner Owner { get; } = owner;
    internal nint Pointer { get; } = pointer;
    internal nint Name { get; } = name;

    internal int TryLock()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(destroyed, this);
            if (holder != 0)
            {
                if (!OperatingSystem.IsWindows() || holder != Environment.CurrentManagedThreadId)
                    return NifNativeMutex795Names.Busy;
                recursion = checked(recursion + 1);

                return 0;
            }
            holder = Environment.CurrentManagedThreadId;
            recursion = 1;

            return 0;
        }
    }

    internal void Lock()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(destroyed, this);
            // Pinned Windows ERTS uses recursive CRITICAL_SECTION operations.
            // POSIX retains its nonrecursive lock behavior.
            if (OperatingSystem.IsWindows() && holder == Environment.CurrentManagedThreadId)
            {
                recursion = checked(recursion + 1);

                return;
            }
            waiters++;
            try
            {
                while (holder != 0)
                {
                    Monitor.Wait(gate);
                    ObjectDisposedException.ThrowIf(destroyed, this);
                }
                holder = Environment.CurrentManagedThreadId;
                recursion = 1;
            }
            finally
            {
                waiters--;
            }
        }
    }

    internal void Unlock()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(destroyed, this);
            if (holder != Environment.CurrentManagedThreadId)
                throw new InvalidOperationException(NifNativeMutex795Names.WrongThread);
            if (--recursion != 0)
                return;
            holder = 0;
            Monitor.Pulse(gate);
        }
    }

    internal void Destroy()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(destroyed, this);
            if (holder != 0 || waiters != 0)
                throw new InvalidOperationException(NifNativeMutex795Names.BusyDestroy);
            destroyed = true;
        }
    }
}
