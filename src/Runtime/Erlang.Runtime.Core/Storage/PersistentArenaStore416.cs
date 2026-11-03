namespace Erlang;

// Isolated candidate backing store. No native registration or lifecycle hook is added.
internal sealed partial class PersistentArenaStore416
{
    private readonly ProcessRuntime runtime;
    private readonly object gate = new();
    private readonly SemaphoreSlim updates = new(1, 1);
    private readonly PersistentArenaAccounting416 accounting = new();
    private PersistentTable416? current;
    private Task retirement = Task.CompletedTask;

    internal PersistentArenaStore416(ProcessRuntime runtime)
    {
        this.runtime = runtime;
        current = PersistentTable416.Create(PersistentTableNames416.InitialCapacity, [], accounting: accounting);
    }

    internal PersistentArenaAllocationReport416 Allocations() => accounting.Snapshot();

    private ulong Hash(Term key)
    {
        using var identities = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);

        return identities.Hash(key);
    }

    internal PersistentArenaInfo416 Info(ProcessContext context)
    {
        runtime.RequirePersistentActor(context);
        lock (gate) return Table().Info();
    }

    internal PersistentTableRead416 AcquireSnapshot()
    {
        lock (gate) return Table().AcquireRead();
    }

    internal Term Get(ProcessContext context, Term key, Term? fallback = null)
    {
        runtime.RequirePersistentActor(context);
        ulong hash = Hash(key);
        using var read = AcquireSnapshot();
        TupleTerm? found = read.FindSelected591(
            key,
            hash,
            context.Cancellation,
            valueOnly: true
        );

        return found?.Items[1] ?? fallback ?? throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    internal Term GetAll(ProcessContext context)
    {
        runtime.RequirePersistentActor(context);

        return AtomicsHostAggregate594.Publish(transaction => GetAllSelected594(context, transaction));
    }

    internal Term GetAllSelected594(ProcessContext context, AtomicsHostCopyTransaction591 transaction)
    {
        runtime.RequirePersistentActor(context);
        using var read = AcquireSnapshot();
        Term result = Nil.Value;
        foreach (var pair in read.PairsSelected594(context.Cancellation, transaction))
            result = new Cons(pair, result);

        return result;
    }

    internal async ValueTask Put(
        ProcessContext context,
        Term key,
        Term value,
        bool newOnly = false
    )
    {
        runtime.RequirePersistentActor(context);
        ulong hash = Hash(key);
        await updates.WaitAsync(context.Cancellation).ConfigureAwait(false);
        PersistentPairArena416? staged = null;
        PersistentPairArena416? replaced = null;
        PersistentTable416? abandoned = null;
        int replacedSlot = -1;
        try
        {
            using var read = AcquireSnapshot();
            TupleTerm? previous = read.FindSelected591(
                key,
                hash,
                context.Cancellation,
                admitResults: false
            );
            if (previous is not null && previous.Items[1].Equals(value))
            {
                runtime.PublishPersistent(context, () =>
 {
     lock (gate)
     {
         _ = Table();

         return true;
     }
 });

                return;
            }
            if (previous is not null && newOnly) throw new ErlangException(ErlangErrorReasons.BadArgument);
            staged = PersistentPairArena416.Capture(
                runtime,
                key,
                value,
                context.Cancellation,
                accounting
            );
            PersistentTableEntry416[] active;
            int capacity;
            lock (gate)
            {
                PersistentTable416 table = Table();
                active = table.Entries();
                capacity = table.Info().Capacity;
            }
            var next = new List<PersistentTableEntry416>(active.Length + 1);
            foreach (var entry in active)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (previous is not null && entry.Hash == hash)
                {
                    using var candidate = entry.Area.AcquireRead();
                    if (candidate.MaterializeSelected591(context.Cancellation, admitResults: false).Items[0].Equals(key))
                    {
                        replaced = entry.Area;
                        replacedSlot = entry.Slot;
                        continue;
                    }
                }
                next.Add(entry);
            }
            int readCapacity = capacity;
            bool inserted = replaced is null;
            if (inserted && active.Length >= capacity / PersistentTableNames416.GrowDivisor)
                capacity = checked(capacity * 2);
            next.Add(new(hash, staged, replacedSlot));
            abandoned = PersistentTable416.Create(
                capacity,
                next,
                capacity == readCapacity,
                accounting
            );

            runtime.PublishPersistent(context, () =>
            {
                lock (gate)
                {
                    PersistentTable416 old = Table();
                    current = abandoned;
                    abandoned = old;
                    staged = null;

                    return true;
                }
            });
        }
        finally
        {
            try
            {
                abandoned?.Retire();
                staged?.Retire();
                // On refused publication the old active pair must remain owned.
                if (staged is null) replaced?.Retire();
            }
            finally
            {
                updates.Release();
            }
        }
    }

    internal async ValueTask<bool> Erase(ProcessContext context, Term key)
    {
        runtime.RequirePersistentActor(context);
        ulong hash = Hash(key);
        await updates.WaitAsync(context.Cancellation).ConfigureAwait(false);
        PersistentPairArena416? removed = null;
        PersistentTable416? staged = null;
        bool published = false;
        int removedSlot = -1;
        try
        {
            PersistentTableEntry416[] active;
            int capacity;
            lock (gate)
            {
                PersistentTable416 table = Table();
                active = table.Entries();
                capacity = table.Info().Capacity;
            }
            var next = new List<PersistentTableEntry416>(active.Length);
            foreach (var entry in active)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (entry.Hash == hash)
                {
                    using var read = entry.Area.AcquireRead();
                    if (read.MaterializeSelected591(context.Cancellation, admitResults: false).Items[0].Equals(key))
                    {
                        removed = entry.Area;
                        removedSlot = entry.Slot;
                        continue;
                    }
                }
                next.Add(entry);
            }
            if (removed is null)
                return runtime.PublishPersistent(context, () =>
 {
     lock (gate)
     {
         _ = Table();

         return false;
     }
 });
            bool shrink = capacity > PersistentTableNames416.InitialCapacity
                && active.Length <= capacity / PersistentTableNames416.ShrinkDivisor;
            bool steppingStone = active.Any(entry => entry.Slot == ((removedSlot + 1) & (capacity - 1)));
            if (shrink) capacity /= 2;
            staged = PersistentTable416.Create(
                capacity,
                next,
                !shrink && !steppingStone,
                accounting
            );
            runtime.PublishPersistent(context, () =>
            {
                lock (gate)
                {
                    PersistentTable416 old = Table();
                    current = staged;
                    staged = old;
                    published = true;

                    return true;
                }
            });

            return true;
        }
        finally
        {
            try
            {
                staged?.Retire();
                if (published) removed?.Retire();
            }
            finally
            {
                updates.Release();
            }
        }
    }

    private PersistentTable416 Table() => !ownerAdmissionClosed417 && current is { } active
        ? active : throw new ObjectDisposedException(PersistentTableNames416.Closed);

    internal Task CloseAsync()
    {
        while (true)
        {
            PersistentTable416 table;
            lock (gate)
            {
                if (current is null) return retirement;
                table = current;
            }
            IReadOnlyList<PersistentPairArena416> owners;
            try
            {
                owners = table.AreaOwnersForRetirement();
            }
            catch (ObjectDisposedException)
            {
                lock (gate) if (!ReferenceEquals(current, table)) continue;
                throw;
            }
            // Prepare failure storage and the async state before changing owner state.
            // This is O(active pairs) metadata preparation, not advertised as O(1).
            var failures = new List<Exception>(checked(owners.Count + 2));
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                if (!ReferenceEquals(current, table)) continue;
                Task next = CloseCore(
                    start.Task,
                    table,
                    owners,
                    failures
                );
                current = null;
                retirement = next;
                start.TrySetResult();

                return next;
            }
        }
    }

    internal async Task WaitForReclamationAsync()
    {
        await CloseAsync().ConfigureAwait(false);
        await accounting.WaitForReclamationAsync().ConfigureAwait(false);
    }

    private async Task CloseCore(
        Task start,
        PersistentTable416 table,
        IReadOnlyList<PersistentPairArena416> owners,
        List<Exception> failures
    )
    {
        await start.ConfigureAwait(false);
        bool joined = false;
        try
        {
            try
            {
                await updates.WaitAsync().ConfigureAwait(false);
                joined = true;
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            for (int index = 0; index < owners.Count; index++)
            {
                try
                {
                    owners[index].Retire();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if ((index + 1) % PersistentTableNames416.RetirementUnits == 0) await Task.Yield();
            }
        }
        finally
        {
            try
            {
                table.Retire();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (joined) updates.Release();
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
