using System.Runtime.InteropServices;

namespace Erlang;

// Counts exact successful native allocation requests until the corresponding real free.
internal sealed partial class PersistentArenaAccounting416
{
    private readonly object gate = new();
    private long pairs, pairBytes, tables, tableBytes;
    private TaskCompletionSource? drained;
    private Exception? releaseFailure;
    private readonly Exception invalidRelease = new InvalidOperationException(PersistentArenaNames416.Invalid);

    internal unsafe nint Allocate(PersistentAllocationKind416 kind, long bytes)
    {
        lock (gate)
        {
            if (bytes <= 0) throw new InvalidOperationException(PersistentArenaNames416.Invalid);
            long count = checked((kind == PersistentAllocationKind416.Pair ? pairs : tables) + 1);
            long extent = checked((kind == PersistentAllocationKind416.Pair ? pairBytes : tableBytes) + bytes);
            TaskCompletionSource? nextDrain = pairs + tables == 0
                ? new(TaskCreationOptions.RunContinuationsAsynchronously) : null;
            owned434.EnsureCapacity(checked(owned434.Count + 1));
            void* pointer = NativeMemory.Alloc(checked((nuint)bytes));
            if (pointer == null) throw new OutOfMemoryException();
            try
            {
                owned434.Add((nint)pointer, (kind, bytes));
            }
            catch
            {
                NativeMemory.Free(pointer);
                throw;
            }
            if (nextDrain is not null) drained = nextDrain;
            if (kind == PersistentAllocationKind416.Pair)
            {
                pairs = count;
                pairBytes = extent;
            }
            else
            {
                tables = count;
                tableBytes = extent;
            }

            return (nint)pointer;
        }
    }

    internal unsafe void Free(PersistentAllocationKind416 kind, nint pointer, long bytes)
    {
        lock (gate)
        {
            // The pointer is exclusively owned by this SafeHandle. Free it even
            // when an internal accounting invariant fails; report that failure.
            bool valid = kind == PersistentAllocationKind416.Pair
                ? pairs > 0 && pairBytes >= bytes : tables > 0 && tableBytes >= bytes;
            valid &= owned434.Remove(pointer, out var actual) && actual.Kind == kind && actual.Bytes == bytes;
            NativeMemory.Free((void*)pointer);
            if (kind == PersistentAllocationKind416.Pair)
            {
                pairs--;
                pairBytes -= bytes;
            }
            else
            {
                tables--;
                tableBytes -= bytes;
            }
            if (!valid) releaseFailure ??= invalidRelease;
            if (pairs + tables == 0) drained?.TrySetResult();
        }
    }

    internal void ReportReleaseFailure(Exception failure)
        => Interlocked.CompareExchange(ref releaseFailure, failure, null);

    internal async Task WaitForReclamationAsync()
    {
        Task pending;
        lock (gate) pending = pairs + tables == 0 ? Task.CompletedTask : drained!.Task;
        await pending.ConfigureAwait(false);
        if (Volatile.Read(ref releaseFailure) is { } failure)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal PersistentArenaAllocationReport416 Snapshot()
    {
        lock (gate) return new(
            pairs,
            pairBytes,
            tables,
            tableBytes
        );
    }
}
