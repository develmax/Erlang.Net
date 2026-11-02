namespace Erlang;

internal sealed partial class NifImageBindings
{
    private NifNativeBindingTransition544? nativeTransition544;
    private bool closePreparation544;

    internal NifNativeBindingTransition544 PrepareNativeTransition544(
        ProcessRuntime runtime,
        NifImageBindingReservation pending,
        NifPreparedImageBindings544 prepared,
        NifNativeLibraryOwner candidate,
        NativeResourceTypeTable527 resourceTable,
        NifResourceImagePin524 loadingAuthority
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(resourceTable);
        ArgumentNullException.ThrowIfNull(loadingAuthority);
        // All promises/composite-drain/retirement storage precede Consume544.
        var transition = new NifNativeBindingTransition544(this, pending, candidate);
        lock (gate)
        {
            if (phase != NifImageBindingPhase.Initializing || closePreparation544
                || nativeTransition544 is not null || !ReferenceEquals(reservation, pending)
                || !ReferenceEquals(resourceNames529, resourceTable)
                || !loadingAuthority.IsActiveFor524(this) || activeCalls == 0
                || !candidate.MatchesNextOwner544(runtime, Image))
                throw new InvalidOperationException(NifNativeTransition544Names.Admission);
            Dictionary<(string Name, int Arity), ErlangFunction> functions = prepared.Consume544(this, pending);
            transition.Arm544(functions, prepared.InstalledFunctions544);
            nativeTransition544 = transition;
        }

        return transition;
    }

    // Caller owns gate. A close that observed no transition marks its slow-path
    // preparation before releasing gate, so activation cannot overtake that close.
    private bool CloseNativeTransition544()
    {
        if (nativeTransition544 is not { } transition) return false;
        callsDrained = transition.CallsDrained544;
        if (activeCalls == 0) callsDrained.TrySetResult();
        retirementState529 = transition.Retirement544;
        retirement = transition.Retirement544.Completion529;
        phase = NifImageBindingPhase.Retired;
        ready = null;
        installedFunctions = Nil.Value;
        nativeOwner = null;

        return true;
    }

    internal bool NativeTransitionIsRetired544(NifNativeBindingTransition544 transition)
    {
        lock (gate)
        {
            return ReferenceEquals(nativeTransition544, transition) && phase == NifImageBindingPhase.Retired;
        }
    }

    // Called ONLY after native zero and ROOT's pre-admitted resource publication
    // has acquired its writer, after releasing the normal scheduler barrier.
    internal void CommitNativeTransition544(
        NifNativeBindingTransition544 transition,
        NativeResourcePublication527 resourcePublication
    )
    {
        lock (gate)
        {
            // These are token-integrity checks, never repeatable admission tests.
            if (!ReferenceEquals(nativeTransition544, transition) || transition.Completed544)
                throw new InvalidOperationException(NifNativeTransition544Names.Foreign);
            // Resource names and functions observe the same binding state.
            // A close after admission installs retired callback ownership.
            resourcePublication.CommitAuthorizedUpgrade544(phase == NifImageBindingPhase.Retired);
            if (phase == NifImageBindingPhase.Retired)
                transition.TransferRetiredOwner544();
            else
            {
                ready = transition.Functions544;
                installedFunctions = transition.Installed544;
                nativeOwner = transition.Candidate544;
                phase = NifImageBindingPhase.Ready;
            }
            // Keep the real pending reservation attached through publication
            // disposal, including a close that starts just after this commit.
            nativeTransition544 = null;
            transition.Complete544();
            // The real reservation driver settles pending only after resource
            // publication disposal has finished and recorded all cleanup errors.
        }
    }

    internal void EndNativeVeto544(NifNativeBindingTransition544 transition)
    {
        lock (gate)
        {
            if (!ReferenceEquals(nativeTransition544, transition) || transition.Completed544)
                throw new InvalidOperationException(NifNativeTransition544Names.Foreign);
            nativeTransition544 = null;
            transition.Complete544();
            // Do not complete pending or transfer candidate cleanup. The actual
            // caller must close its candidate before disposing this reservation.
        }
    }
}
