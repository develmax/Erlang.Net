namespace Erlang;

internal static class NifNativeImageInitializer
{
    // Called only by the existing reservation driver while it holds its code permission.
    internal static async ValueTask<NifImageInitialized> InitializeAsync(
        NifImageLoadReservation reservation,
        NifNativeCapabilityProfile profile,
        Term loadInfo,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(profile);
        var owner = new NifNativeLibraryOwner(reservation.Image, reservation.Generation.Context);
        owner.RequiresUpgrade544 = reservation.Generation.PreviousNativeImage544 is not null;
        RuntimeNormalBarrierRequest696? barrier = null;
        NifNativePreviousOwnerLease544? previous = null;
        try
        {
            NativeResourceImages524.Register524(owner.ResourceAdapter524());
            owner.BeginResourceLoad527(reservation);
            if (reservation.Generation.PreviousNativeImage544 is { } previousImage)
            {
                ProcessContext context = reservation.Generation.Context;
                previous = previousImage.NifBindings.RetainPreviousNativeOwner544(context.Runtime, reservation.Image);
                // Snapshot resources before stopping normal progress: a dirty
                // callback holding a reader may need another normal actor.
                NativeResourceUpgradeCommit544 resources = owner.PrepareResourceUpgrade544();
                barrier = await context.Runtime.AcquireNormalBarrier696(context);
                owner.UpgradeOperation544 = new(
                    reservation,
                    barrier,
                    previous,
                    resources
                );
            }
            var bindings = owner.Initialize(
                profile,
                reservation.Generation.TargetDescriptor,
                loadInfo,
                cancellation
            );

            owner.EndResourceLoadCallback527();

            return owner.UpgradeOperation544 is { NativeSucceeded544: true } operation
                ? operation.Commit544() : new NifImageInitialized(owner, bindings);
        }
        catch (Exception failure)
        {
            barrier?.Dispose();
            // Native zero may have freed old storage. Never send this owner
            // through the uncommitted load rollback path after that cutpoint.
            if (owner.UpgradeOperation544 is { NativeSucceeded544: true } succeeded)
            {
                if (succeeded.Published544) throw;
                // Environment/scope unwinding can fail after the callback has
                // returned zero but before Initialize returns its carrier.
                NifImageInitialized committed = succeeded.Commit544();
                committed.NativePostCommitFailure544 = committed.NativePostCommitFailure544 is { } previousFailure
                    ? new AggregateException(previousFailure, failure) : failure;

                return committed;
            }
            try
            {
                owner.UpgradeOperation544?.EndVeto544();
                owner.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(failure, cleanupFailure);
            }
            throw;
        }
        finally
        {
            barrier?.Dispose();
            previous?.Dispose();
        }
    }
}
