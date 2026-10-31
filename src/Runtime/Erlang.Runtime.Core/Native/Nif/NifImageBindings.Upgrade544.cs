namespace Erlang;

internal sealed partial class NifImageBindings
{
    internal NifNativePreviousOwnerLease544 RetainPreviousNativeOwner544(
        ProcessRuntime runtime,
        ModuleCodeImage nextImage
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(nextImage);
        // Allocate both objects before admitting the physical image reference.
        var pin = new NifResourceImagePin524(this);
        var lease = new NifNativePreviousOwnerLease544(
            this,
            pin,
            runtime,
            nextImage
        );
        lock (gate)
        {
            if (phase != NifImageBindingPhase.Ready || nativeOwner is not NifNativeLibraryOwner previous
                || !previous.MatchesPreviousOwner544(runtime, Image, nextImage))
                throw new InvalidOperationException(NifNativeUpgrade544Names.PreviousUnavailable);
            activeCalls = checked(activeCalls + 1);
            pin.Admit524();
            lease.Admit544(previous);
        }

        return lease;
    }
}
