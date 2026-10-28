namespace Erlang;

// Internal lifetime primitive only. Registry code-modification admission and entry routing
// must be integrated before any native loader can publish bindings through this primitive.
internal sealed partial class NifImageBindings
{
    internal NifImageBindings(ModuleCodeImage image) => Image = image;

    internal ModuleCodeImage Image
    {
        get;
    }

    private readonly object gate = new();
    private NifImageBindingPhase phase;
    private NifImageBindingReservation? reservation;
    private Dictionary<(string Name, int Arity), ErlangFunction>? ready;
    private Term installedFunctions = Nil.Value;
    private IAsyncDisposable? nativeOwner;
    private int activeCalls;
    private Exception? physicalReleaseFailure521;
    private Exception? retainedPhysicalFailure521;
    private TaskCompletionSource? callsDrained;
    private TaskCompletionSource? retirement;
    private Task? retirementDriver;

    internal NifImageBindingPhase Phase
    {
        get
        {
            lock (gate)
                return phase;
        }
    }

    internal Term InstalledFunctions
    {
        get
        {
            lock (gate)
                return installedFunctions;
        }
    }

    internal Task? RetirementDriver
    {
        get
        {
            lock (gate)
                return retirementDriver;
        }
    }

    internal NifImageBindingReservation Begin(ModuleCodeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var pending = new NifImageBindingReservation(this, descriptor);
        lock (gate)
        {
            if (phase == NifImageBindingPhase.Retired)
                throw new InvalidOperationException(NifImageBindingNames.Retired);
            if (phase != NifImageBindingPhase.Empty)
                throw new InvalidOperationException(NifImageBindingNames.Busy);
            reservation = pending;
            phase = NifImageBindingPhase.Initializing;
        }

        return pending;
    }

    internal void Publish(
        NifImageBindingReservation pending,
        IEnumerable<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings,
        IAsyncDisposable owner
    )
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(owner);
        var snapshot = new Dictionary<(string Name, int Arity), ErlangFunction>();
        var ordered = new List<Term>();
        foreach (var binding in bindings)
        {
            if (binding.Value is null || !pending.Eligible.Contains(binding.Key))
                throw new ArgumentException(NifImageBindingNames.InvalidFunction, nameof(bindings));
            if (!snapshot.TryAdd(binding.Key, binding.Value))
                throw new ArgumentException(NifImageBindingNames.DuplicateFunction, nameof(bindings));
            ordered.Add(Term.Tuple(Term.A(binding.Key.Name), Term.I(binding.Key.Arity)));
        }
        Term installed = Cons.From(ordered);
        using NativeResourcePublication527? resourcePublication =
            (owner as NifNativeLibraryOwner)?.PrepareResourcePublication527();
        lock (gate)
        {
            if (phase != NifImageBindingPhase.Initializing || !ReferenceEquals(reservation, pending))
                throw new InvalidOperationException(NifImageBindingNames.ForeignReservation);
            ready = snapshot;
            installedFunctions = installed;
            nativeOwner = owner;
            reservation = null;
            phase = NifImageBindingPhase.Ready;
            resourcePublication?.Commit527();
            pending.Complete();
        }
    }

    internal void Abort(NifImageBindingReservation pending)
    {
        lock (gate)
        {
            if (ReferenceEquals(reservation, pending))
            {
                reservation = null;
                if (phase == NifImageBindingPhase.Initializing)
                    phase = NifImageBindingPhase.Empty;
            }
            pending.Complete();
        }
    }

    internal NifImageCall? Admit(string function, int arity)
    {
        lock (gate)
        {
            if (phase == NifImageBindingPhase.Retired)
                throw new InvalidOperationException(NifImageBindingNames.Retired);
            if (phase != NifImageBindingPhase.Ready || !ready!.TryGetValue((function, arity), out var body))
                return null;
            checked
            {
                activeCalls++;
            }

            return new NifImageCall(
                this,
                body,
                arity,
                IsGuestNativeBody535(function, arity, body)
            );
        }
    }

    // Allocation-free diagnostic assignment. A failed final release intentionally
    // retains activeCalls/nativeOwner; never manufacture a successful drain.
    internal Exception? RetainedPhysicalFailure521 => Volatile.Read(ref retainedPhysicalFailure521);

    internal void RecordPhysicalRetention521(Exception failure)
        => Interlocked.CompareExchange(ref retainedPhysicalFailure521, failure, null);

    internal void ReleaseCall(Exception? physicalFailure521 = null)
    {
        lock (gate)
        {
            if (activeCalls == 0)
                throw new InvalidOperationException(NifImageBindingNames.ActiveUnderflow);
            if (physicalFailure521 is not null)
                physicalReleaseFailure521 = physicalReleaseFailure521 is null ? physicalFailure521
                    : new AggregateException(physicalReleaseFailure521, physicalFailure521);
            activeCalls--;
            if (activeCalls == 0)
                callsDrained?.TrySetResult();
        }
    }

    internal Task RetireAsync()
    {
        CloseAdmission529();
        NifImageRetirementState529 state;
        lock (gate)
        {
            state = retirementState529!;
            if (state.DriverStarted529) return state.Completion529.Task;
            state.DriverStarted529 = true;
        }
        Exception? preparationFailure = null;
        try
        {
            // Empty type pins and owner cancellation settle outside all name,
            // binding and registry gates on the physical retirement worker.
            state.Names529?.Finish529();
            (state.Cleanup529 as INifImageRetirementPreparation)?.BeginRetirement();
        }
        catch (Exception error)
        {
            preparationFailure = error;
        }
        Task driver = CompleteRetirement(
            state.Drain529,
            state.Cleanup529,
            state.Completion529,
            preparationFailure
        );
        lock (gate)
            retirementDriver = driver;

        return state.Completion529.Task;
    }

    private async Task CompleteRetirement(
        Task drain,
        IAsyncDisposable? cleanup,
        TaskCompletionSource completion,
        Exception? preparationFailure
    )
    {
        var errors = new List<Exception>();
        if (preparationFailure is not null) errors.Add(preparationFailure);
        try
        {
            await drain.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        lock (gate)
        {
            if (physicalReleaseFailure521 is not null)
                errors.Add(physicalReleaseFailure521);
            if (resourcePublicationFailures527 is { } resourceFailures)
                foreach (Exception? failure in resourceFailures)
                    if (failure is not null) errors.Add(failure);
        }
        try
        {
            if (cleanup is not null) await cleanup.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (errors.Count == 0) completion.TrySetResult();
        else completion.TrySetException(errors.Count == 1 ? errors[0] : new AggregateException(errors));
    }
}
