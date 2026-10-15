namespace Erlang;

// Internal lifetime primitive only. Registry code-modification admission and entry routing
// must be integrated before any native loader can publish bindings through this primitive.
internal sealed class NifImageBindings
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
        lock (gate)
        {
            if (phase != NifImageBindingPhase.Initializing || !ReferenceEquals(reservation, pending))
                throw new InvalidOperationException(NifImageBindingNames.ForeignReservation);
            ready = snapshot;
            installedFunctions = installed;
            nativeOwner = owner;
            reservation = null;
            phase = NifImageBindingPhase.Ready;
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

            return new NifImageCall(this, body, arity);
        }
    }

    internal void ReleaseCall()
    {
        lock (gate)
        {
            if (activeCalls == 0)
                throw new InvalidOperationException(NifImageBindingNames.ActiveUnderflow);
            activeCalls--;
            if (activeCalls == 0)
                callsDrained?.TrySetResult();
        }
    }

    internal Task RetireAsync()
    {
        IAsyncDisposable? cleanup;
        Task drain;
        TaskCompletionSource completion;
        lock (gate)
        {
            if (retirement is not null)
                return retirement.Task;
            retirement = completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            phase = NifImageBindingPhase.Retired;
            ready = null;
            installedFunctions = Nil.Value;
            cleanup = nativeOwner;
            nativeOwner = null;
            if (activeCalls == 0)
                drain = Task.CompletedTask;
            else
            {
                callsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
                drain = callsDrained.Task;
            }
            if (reservation is { } initializing)
                drain = Task.WhenAll(drain, initializing.Settled);
        }
        Task driver = CompleteRetirement(drain, cleanup, completion);
        lock (gate)
            retirementDriver = driver;

        return completion.Task;
    }

    private static async Task CompleteRetirement(Task drain, IAsyncDisposable? cleanup, TaskCompletionSource completion)
    {
        try
        {
            await drain.ConfigureAwait(false);
            if (cleanup is not null)
                await cleanup.DisposeAsync().ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception failure)
        {
            completion.TrySetException(failure);
        }
    }
}
