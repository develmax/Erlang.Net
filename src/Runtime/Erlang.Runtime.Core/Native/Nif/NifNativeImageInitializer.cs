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
        var cleanupErrors616 = new Exception?[NifNativePhaseNames616.InitializationFailureSlots];

        void ReleaseBarrier616()
        {
            RuntimeNormalBarrierRequest696? pending = barrier;
            barrier = null;
            try
            {
                owner.UpgradeOperation544?.ReleaseNormalBarrier544();
            }
            finally
            {
                pending?.Dispose();
            }
        }

        static Exception Failure616(Exception primary, Exception?[] cleanup)
        {
            Exception[] errors = cleanup.Where(error => error is not null).Cast<Exception>().ToArray();

            return errors.Length == 0 ? primary : new AggregateException(new[] { primary }.Concat(errors));
        }
        try
        {
            NativeResourceImages524.Register524(owner.ResourceAdapter524());
            owner.BeginResourceLoad527(reservation);
            // The durable permit spans phases, but never installs a physical
            // thread-local scope or holds an owner monitor across an await.
            using NifNativeLifecyclePermit616 permit = await NifNativeLifecycleGate.AcquireAsync616(cancellation);
            NativeResourceUpgradeCommit544? resources = null;
            if (reservation.Generation.PreviousNativeImage544 is { } previousImage)
            {
                ProcessContext context = reservation.Generation.Context;
                previous = previousImage.NifBindings.RetainPreviousNativeOwner544(context.Runtime, reservation.Image);
                resources = owner.PrepareResourceUpgrade544();
            }
            owner.OpenAndValidate616(
                profile,
                reservation.Generation.TargetDescriptor,
                cancellation,
                permit
            );
            IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings;
            if (previous is not null)
            {
                ProcessContext context = reservation.Generation.Context;
                owner.UpgradeOperation544 = new(reservation, previous, resources!);
                owner.PrepareUpgrade616();
                // Every lifecycle contender now awaits admission; none can
                // occupy a normal lane blocking on the global semaphore.
                barrier = await context.Runtime.AcquireNifBarrier616(context);
                owner.UpgradeOperation544.AttachNormalBarrier616(barrier);
                barrier = null; // Actual grant ownership transferred exactly once.
                bindings = owner.InitializeUpgrade616(loadInfo, cancellation, permit);
            }
            else
                bindings = owner.InitializeLoad616(loadInfo, cancellation, permit);

            owner.EndResourceLoadCallback527();

            return owner.UpgradeOperation544 is { NativeSucceeded544: true } operation
                ? operation.Commit544() : new NifImageInitialized(owner, bindings);
        }
        catch (Exception failure)
        {
            try
            {
                ReleaseBarrier616();
            }
            catch (Exception cleanupFailure)
            {
                cleanupErrors616[NifNativePhaseNames616.PendingBarrierFailure] = cleanupFailure;
            }
            // Native zero may have freed old storage. Never send this owner
            // through the uncommitted load rollback path after that cutpoint.
            if (owner.UpgradeOperation544 is { NativeSucceeded544: true } succeeded)
            {
                if (succeeded.Published544)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(Failure616(failure, cleanupErrors616)).Throw();
                // Environment/scope unwinding can fail after the callback has
                // returned zero but before Initialize returns its carrier.
                NifImageInitialized committed = succeeded.Commit544();
                Exception reportedFailure616 = Failure616(failure, cleanupErrors616);
                committed.NativePostCommitFailure544 = committed.NativePostCommitFailure544 is { } previousFailure
                    ? new AggregateException(previousFailure, reportedFailure616) : reportedFailure616;

                return committed;
            }
            try
            {
                owner.UpgradeOperation544?.EndVeto544();
            }
            catch (Exception cleanupFailure)
            {
                cleanupErrors616[NifNativePhaseNames616.VetoEndFailure] = cleanupFailure;
            }
            try
            {
                await owner.DisposeAsync();
            }
            catch (Exception cleanupFailure)
            {
                cleanupErrors616[NifNativePhaseNames616.OwnerRollbackFailure] = cleanupFailure;
            }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(Failure616(failure, cleanupErrors616)).Throw();
            throw;
        }
        finally
        {
            try
            {
                ReleaseBarrier616();
            }
            finally
            {
                previous?.Dispose();
            }
        }
    }
}
