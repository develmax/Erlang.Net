namespace Erlang;

// Created before native code can mutate either private-data cell. Until zero,
// the initializer owns rollback; after zero, only committed retirement owns it.
internal sealed class NifNativeUpgradeOperation544(
    NifImageLoadReservation reservation,
    NifNativePreviousOwnerLease544 previous,
    NativeResourceUpgradeCommit544 resourceCommit
)
{
    private NifNativeBindingTransition544? transition;
    private bool vetoEnded;
    private RuntimeNormalBarrierRequest696? barrier616;
    private int barrierAttached616;
    internal NifNativePreviousOwnerLease544 Previous544 => previous;
    internal bool NativeSucceeded544 { get; set; }
    internal NifImageInitialized Result544 { get; private set; } = null!;
    internal bool Published544 => Result544 is { NativePublished544: true };

    internal void Prepare544(
        NifNativeLibraryOwner owner,
        IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings,
        NativeResourceLoadTransaction527 resources
    )
    {
        Result544 = new(owner, bindings);
        NifPreparedImageBindings544 prepared = reservation.ResourceBindingReservation527.Prepare(bindings);
        reservation.AttachNativeUpgrade544(this);
        transition = reservation.PrepareNativeUpgrade544(prepared, owner, resources);
    }

    internal void AttachNormalBarrier616(RuntimeNormalBarrierRequest696 barrier)
    {
        ArgumentNullException.ThrowIfNull(barrier);
        if (Interlocked.CompareExchange(ref barrierAttached616, 1, 0) != 0)
            throw new InvalidOperationException(NifNativeTransition544Names.Admission);
        barrier616 = barrier;
    }

    internal void ReleaseNormalBarrier544() => Interlocked.Exchange(ref barrier616, null)?.Dispose();

    internal void EndVeto544()
    {
        if (transition is null || vetoEnded) return;
        transition.EndVeto544();
        vetoEnded = true;
    }

    internal NifImageInitialized Commit544()
    {
        using (NativeResourcePublication527 publication = resourceCommit.AcquireAfterSuccess544())
        {
            transition!.Commit544(publication);
            Result544.NativePublished544 = true;
        }

        return Result544;
    }
}
