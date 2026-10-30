namespace Erlang;

internal sealed class NifNativeBindingTransition544 : IAsyncDisposable
{
    private readonly NifImageBindings owner544;
    private Dictionary<(string Name, int Arity), ErlangFunction>? functions544;
    private Term installed544 = Nil.Value;
    private NifNativeLibraryOwner? committedRetiredOwner544;

    internal NifNativeBindingTransition544(
        NifImageBindings owner,
        NifImageBindingReservation pending,
        NifNativeLibraryOwner candidate
    )
    {
        owner544 = owner;
        Pending544 = pending;
        Candidate544 = candidate;
        CallsDrained544 = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task drain = Task.WhenAll(CallsDrained544.Task, pending.Settled);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Retirement544 = new(
            this,
            drain,
            completion,
            null
        );
    }

    internal NifImageBindingReservation Pending544 { get; }
    internal NifNativeLibraryOwner Candidate544 { get; }
    internal TaskCompletionSource CallsDrained544 { get; }
    internal NifImageRetirementState529 Retirement544 { get; }
    internal Dictionary<(string Name, int Arity), ErlangFunction> Functions544 => functions544!;
    internal Term Installed544 => installed544;
    internal bool Completed544 { get; private set; }
    internal bool Retired544 => owner544.NativeTransitionIsRetired544(this);

    internal void Arm544(Dictionary<(string Name, int Arity), ErlangFunction> functions, Term installed)
    {
        functions544 = functions;
        installed544 = installed;
    }

    internal void TransferRetiredOwner544() => committedRetiredOwner544 = Candidate544;

    internal void Complete544() => Completed544 = true;

    internal void Commit544(NativeResourcePublication527 publication)
        => owner544.CommitNativeTransition544(this, publication);

    internal void EndVeto544() => owner544.EndNativeVeto544(this);

    // This proxy deliberately does not implement INifImageRetirementPreparation.
    // ROOT's close-aware resource commit, not a late callback here, retires names.
    public ValueTask DisposeAsync()
    {
        NifNativeLibraryOwner? cleanup = Volatile.Read(ref committedRetiredOwner544);

        return cleanup is null ? ValueTask.CompletedTask : cleanup.DisposeAsync();
    }
}
