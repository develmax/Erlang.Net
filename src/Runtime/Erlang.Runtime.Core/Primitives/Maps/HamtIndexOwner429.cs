using System.Runtime.CompilerServices;

namespace Erlang;

internal sealed class HamtIndexOwner429(ProcessRuntime runtime)
{
    private readonly object gate = new();
    internal object Domain { get; } = new();
    private readonly ConditionalWeakTable<MapTerm, HamtIndex429> maps = new();
    private readonly HashSet<HamtHashLeaseRow429> live = new();
    private readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Exception> failures = new();
    private bool closing, releasing;
    private int readers, builders;
    internal Task Completion => settled.Task;
    internal int LiveLeaseRows
    {
        get
        {
            lock (gate) return live.Count;
        }
    }
    internal bool Closing
    {
        get
        {
            lock (gate) return closing;
        }
    }

    internal HamtIndexRead429 Acquire(MapTerm map)
    {
        if (map.Entries.Count <= HamtViewLayout.ReleaseFlatMapLimit)
            throw new ArgumentException(HamtIndexNames429.Flat);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);
            if (maps.TryGetValue(map, out var existing))
            {
                var read = new HamtIndexRead429(this, existing);
                readers = checked(readers + 1);

                return read;
            }
            if (NativeMapMaterialization505.TryIndex(
                map,
                runtime,
                Domain,
                out var preserved
            ))
            {
                var read = new HamtIndexRead429(this, preserved);
                int admitted = checked(readers + 1);
                maps.Add(map, preserved);
                readers = admitted;

                return read;
            }
            builders = checked(builders + 1);
        }
        HamtIndex429? built = null;
        try
        {
            // No owner lock is held while the actual runtime/hash identity
            // provider takes its runtime or KnownNodes locks.
            using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
            HamtViewEntry[] entries = map.Entries.Select(pair => new HamtViewEntry(pair, hashes.Hash(pair.Key))).ToArray();
            HamtHashLeases429 ownership = hashes.TransferHamtLeases429(this);
            try
            {
                HamtViewNode root = HamtCursorView.Branch(
                    entries,
                    0,
                    Domain,
                    ownership
                );
                built = new(root, ownership, entries.Length);
            }
            catch
            {
                ownership.Dispose();
                throw;
            }
            HamtIndex429? winner;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);
                if (!maps.TryGetValue(map, out winner))
                {
                    // All potentially throwing allocations precede publication.
                    var candidate = new HamtIndexRead429(this, built);
                    int admitted = checked(readers + 1);
                    maps.Add(map, built);
                    readers = admitted;
                    built = null;

                    return candidate;
                }
            }
            // A losing private build retires before admitting a winner's reader.
            // An unexpected retirement error cannot strand an unreturned read.
            built.Ownership.Dispose();
            built = null;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);
                var admitted = new HamtIndexRead429(this, winner);
                readers = checked(readers + 1);

                return admitted;
            }
        }
        finally
        {
            try
            {
                built?.Ownership.Dispose();
            }
            finally
            {
                lock (gate) builders--;
                TrySettle();
            }
        }
    }

    internal HamtNodeRead785 AcquireNode785(HamtBitmapTerm429 node)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);
            if (!ReferenceEquals(Domain, node.Domain))
                throw new ArgumentException(HamtIndexNames429.Foreign);
            var read = new HamtNodeRead785(this);
            readers = checked(readers + 1);

            return read;
        }
    }

    internal void Release()
    {
        lock (gate)
        {
            if (readers <= 0) throw new InvalidOperationException(HamtIndexNames429.Unbalanced);
            readers--;
        }
        TrySettle();
    }

    internal Task Close()
    {
        lock (gate) closing = true;
        TrySettle();

        return settled.Task;
    }

    private void TrySettle()
    {
        HamtHashLeaseRow429[] owned;
        lock (gate)
        {
            if (!closing || readers != 0 || builders != 0 || releasing) return;
            owned = live.ToArray();
            releasing = true;
            maps.Clear();
        }
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = suppress ? ExecutionContext.SuppressFlow() : default;
        try
        {
            _ = CompleteRelease(owned);
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    private async Task CompleteRelease(HamtHashLeaseRow429[] owned)
    {
        try
        {
            await Task.WhenAll(owned.Select(row => row.BeginRelease())).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            lock (gate) failures.Add(error);
        }
        lock (gate)
        {
            if (failures.Count == 0) settled.TrySetResult();
            else settled.TrySetException(new AggregateException(failures));
        }
    }

    internal void RegisterLeases(HamtHashLeaseRow429 row)
    {
        // An already admitted builder may register after Close. Its real builder
        // count keeps shutdown pending until the builder retires/publishes.
        lock (gate) live.Add(row);
    }

    internal void LeaseReleased(HamtHashLeaseRow429 row, Exception? error)
    {
        lock (gate)
        {
            if (error is not null) failures.Add(error);
            live.Remove(row);
        }
    }
}
