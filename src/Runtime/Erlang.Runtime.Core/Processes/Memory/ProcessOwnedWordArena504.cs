using System.Runtime.InteropServices;

namespace Erlang;

// Actual native word storage plus actual GCHandle roots. This is a foundation for
// process-owned VM slots, not an ERTS heap-size estimate or a process_info provider.
internal sealed unsafe class ProcessOwnedWordArena504 : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<long, ProcessOwnedWordLease504> leases = new();
    private readonly TaskCompletionSource drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly long maximumWords;
    private long identity;
    private long reservedWords;
    private long rootedWords;
    private bool closing;

    internal ProcessOwnedWordArena504(long maximumWords = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumWords);
        this.maximumWords = maximumWords;
    }

    internal ProcessOwnedWordSnapshot504 Snapshot
    {
        get
        {
            lock (gate)
                return new(
                    IntPtr.Size,
                    reservedWords,
                    rootedWords,
                    leases.Count,
                    closing
                );
        }
    }

    internal ProcessOwnedWordLease504 Allocate(int words)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(words);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (maximumWords != 0 && words > maximumWords - reservedWords)
                throw new ProcessOwnedWordLimit504(reservedWords, words, maximumWords);
            long nextReserved = checked(reservedWords + words);
            long nextIdentity = checked(identity + 1);
            nint pointer = words == 0 ? 0 : (nint)NativeMemory.AllocZeroed((nuint)words, (nuint)IntPtr.Size);
            try
            {
                var lease = new ProcessOwnedWordLease504(
                    this,
                    nextIdentity,
                    pointer,
                    words
                );
                leases.Add(nextIdentity, lease);
                reservedWords = nextReserved;
                identity = nextIdentity;

                return lease;
            }
            catch
            {
                NativeMemory.Free((void*)pointer);
                throw;
            }
        }
    }

    internal Task RetireAsync()
    {
        lock (gate)
        {
            closing = true;
            if (leases.Count == 0)
                drained.TrySetResult();

            return drained.Task;
        }
    }

    public ValueTask DisposeAsync() => new(RetireAsync());

    internal object? Read(ProcessOwnedWordLease504 lease, int index)
    {
        lock (gate)
        {
            Require(lease, index);
            nint handle = ((nint*)lease.Pointer)[index];

            return handle == 0 ? null : GCHandle.FromIntPtr(handle).Target;
        }
    }

    internal void Write(ProcessOwnedWordLease504 lease, int index, object? value)
    {
        lock (gate)
        {
            Require(lease, index);
            nint previous = ((nint*)lease.Pointer)[index];
            nint replacement = value is null ? 0 : GCHandle.ToIntPtr(GCHandle.Alloc(value, GCHandleType.Normal));
            ((nint*)lease.Pointer)[index] = replacement;
            if (previous != 0)
            {
                GCHandle.FromIntPtr(previous).Free();
                rootedWords--;
            }
            if (replacement != 0)
                rootedWords++;
        }
    }

    internal void Release(ProcessOwnedWordLease504 lease)
    {
        lock (gate)
        {
            if (!leases.TryGetValue(lease.Identity, out var current) || !ReferenceEquals(current, lease))
                return;
            for (int index = 0; index < lease.Words; index++)
            {
                nint handle = ((nint*)lease.Pointer)[index];
                if (handle == 0)
                    continue;
                GCHandle.FromIntPtr(handle).Free();
                rootedWords--;
            }
            NativeMemory.Free((void*)lease.Pointer);
            lease.Pointer = 0;
            leases.Remove(lease.Identity);
            reservedWords -= lease.Words;
            if (closing && leases.Count == 0)
                drained.TrySetResult();
        }
    }

    private void Require(ProcessOwnedWordLease504 lease, int index)
    {
        if (!leases.TryGetValue(lease.Identity, out var current) || !ReferenceEquals(current, lease))
            throw new ObjectDisposedException(nameof(ProcessOwnedWordLease504));
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, lease.Words);
    }
}
