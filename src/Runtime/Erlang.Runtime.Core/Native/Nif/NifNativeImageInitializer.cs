namespace Erlang;

internal static class NifNativeImageInitializer
{
    // Called only by the existing reservation driver while it holds its code permission.
    internal static ValueTask<NifImageInitialized> InitializeAsync(
        NifImageLoadReservation reservation,
        NifNativeCapabilityProfile profile,
        Term loadInfo,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(profile);
        var owner = new NifNativeLibraryOwner(reservation.Image, reservation.Generation.Context);
        try
        {
            var bindings = owner.Initialize(
                profile,
                reservation.Generation.TargetDescriptor,
                loadInfo,
                cancellation
            );

            return ValueTask.FromResult(new NifImageInitialized(owner, bindings));
        }
        catch (Exception failure)
        {
            try
            {
                owner.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception cleanupFailure)
            {
                return ValueTask.FromException<NifImageInitialized>(new AggregateException(failure, cleanupFailure));
            }

            return ValueTask.FromException<NifImageInitialized>(failure);
        }
    }
}
