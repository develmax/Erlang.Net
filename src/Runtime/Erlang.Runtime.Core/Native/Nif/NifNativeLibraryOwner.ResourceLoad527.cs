namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner : INifImageRetirementPreparation
{
    private NativeResourceLoadTransaction527? resourceLoad527;

    internal void BeginResourceLoad527(NifImageLoadReservation reservation)
    {
        NifNativeResourceImage524 provider = NativeResourceImages524.Get524(image);
        NativeResourceLoadTransaction527 transaction = initializationContext.Runtime.NativeResources524
            .ResourceTypes527.BeginLoad527(provider, reservation.ResourceBindingReservation527);
        resourceLoad527 = transaction;
        provider.ResourceLoad527 = transaction;
    }

    internal void EndResourceLoadCallback527() => resourceLoad527?.EndLoading527();

    internal NativeResourceUpgradeCommit544 PrepareResourceUpgrade544()
        => resourceLoad527!.PrepareUpgradeCommit544();

    internal void RollbackResourceLoad527() => resourceLoad527?.Dispose();

    internal NativeResourcePublication527? PrepareResourcePublication527() => resourceLoad527?.PreparePublication527();

    void INifImageRetirementPreparation.BeginRetirement()
        => resourceLoad527?.Table527.RetireImage527(image);
}
