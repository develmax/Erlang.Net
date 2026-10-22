namespace Erlang;

internal sealed partial class RuntimePersistentTerms
{
    private readonly object gate = new();
    private readonly PersistentArenaStore416 arena;
    private readonly TaskCompletionSource<bool> closeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task retirement;
    private bool ownerClosing;

    internal RuntimePersistentTerms(ProcessRuntime runtime)
    {
        // Prepare the tracked async state before acquiring the first native block.
        retirement = PersistentTermOwnerFlow417.Start(RetireOnRequest);
        try
        {
            arena = new(runtime);
        }
        catch
        {
            closeRequested.TrySetResult(false);
            throw;
        }
    }

    internal bool RetirementCompletedSuccessfully
    {
        get
        {
            lock (gate) return !ownerClosing || retirement.IsCompletedSuccessfully;
        }
    }

    internal Task? RequestedRuntimeRetirement417
    {
        get
        {
            lock (gate) return ownerClosing ? retirement : null;
        }
    }

    internal Term Get(ProcessContext context, Term key) => arena.Get(context, key);

    internal Term Get(ProcessContext context, Term key, Term fallback) => arena.Get(context, key, fallback);

    internal Term GetAll(ProcessContext context) => arena.GetAll(context);

    internal async ValueTask<Term> Put(
        ProcessContext context,
        Term key,
        Term value,
        bool newOnly
    )
    {
        await arena.Put(
            context,
            key,
            value,
            newOnly
        ).ConfigureAwait(false);

        return PersistentTermNativeAtoms.Ok;
    }

    internal async ValueTask<Term> Erase(ProcessContext context, Term key)
        => await arena.Erase(context, key).ConfigureAwait(false) ? PersistentTermNativeAtoms.True : PersistentTermNativeAtoms.False;

    internal Term Info(ProcessContext context)
    {
        PersistentArenaInfo416 info = arena.Info(context);

        return new MapTerm([
            new KeyValuePair<Term, Term>(Term.A(PersistentArenaNames416.Count), new Integer(info.Count)),
            new KeyValuePair<Term, Term>(Term.A(PersistentArenaNames416.Memory), new Integer(info.Memory))
        ]);
    }

    private PersistentTermArenaSnapshot417 Snapshot()
    {
        var read = arena.AcquireSnapshot();
        try
        {
            return new(read);
        }
        catch
        {
            read.Dispose();
            throw;
        }
    }

    internal PersistentTermArenaReport417 OwnedStorageReport()
    {
        PersistentArenaOwnerState417 state = arena.OwnerState417();
        var allocations = state.Allocations;

        return new(
            state.Active?.Count ?? 0,
            state.Active?.ActualTableRequestedBytes ?? 0,
            state.Active?.ActivePairRequestedBytes ?? 0,
            allocations.AllocatedPairBlocks,
            allocations.AllocatedPairBytes,
            allocations.AllocatedTableBlocks,
            allocations.AllocatedTableBytes,
            state.Closed
        );
    }

    // Trusted internal/manual retirement preserves the historical held-reader
    // diagnostic surface. Runtime shutdown always uses BeginRuntimeClose417.
    internal Task CloseAsync()
    {
        arena.CloseOwnerAdmission417();

        return arena.CloseAsync();
    }

    internal Task BeginRuntimeClose417()
    {
        lock (gate)
        {
            if (!ownerClosing)
            {
                arena.CloseOwnerAdmission417();
                ownerClosing = true;
                closeRequested.TrySetResult(true);
            }

            return retirement;
        }
    }

    private async Task RetireOnRequest()
    {
        if (!await closeRequested.Task.ConfigureAwait(false)) return;
        // O(active-pairs) preparation and reader drain execute outside runtime gate.
        await arena.CloseAsync().ConfigureAwait(false);
        await arena.WaitForReclamationAsync().ConfigureAwait(false);
    }
}
