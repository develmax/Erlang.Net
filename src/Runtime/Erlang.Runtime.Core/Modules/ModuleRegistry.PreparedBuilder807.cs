namespace Erlang;

public sealed partial class ModuleRegistry
{
    private PreparedLoaderGrant807 IssuePreparedLoaderGrant807(PreparedBeamImage image)
    {
        lock (gate)
        {
            RequirePreparedLoaderImage807(image);
            ModuleLiteralLease800? literals = image.LiteralDomain800?.AcquireForImage800(image.ExecutionIdentity.Image);
            try
            {
                var grant = new PreparedLoaderGrant807(this, image, literals);
                preparedLoaderBuilders807.Add(grant);

                return grant;
            }
            catch
            {
                literals?.Dispose();
                throw;
            }
        }
    }

    private PreparedLoaderReservation807 CompletePreparedLoaderRegistration807(PreparedLoaderGrant807 grant, PreparedLoaderOwner807 candidate)
    {
        Task registration = Task.Run(() => RegisterPreparedLoaderCandidate807(grant, candidate));
        _ = registration.ContinueWith(
            static completed =>
 {
     _ = completed.Exception;
 },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
        Task winner = Task.WhenAny(registration, grant.Closed807.Task).GetAwaiter().GetResult();
        if (!ReferenceEquals(winner, registration))
            throw new ObjectDisposedException(PreparedLoaderNames807.Closed);
        registration.GetAwaiter().GetResult();

        return grant.TakeReservation807() ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
    }

    private void RegisterPreparedLoaderCandidate807(PreparedLoaderGrant807 grant, PreparedLoaderOwner807 candidate)
    {
        // This worker performs only the ordinary metadata decision. Shutdown
        // invalidates the authentic grant under M; after that this callback
        // cannot touch an aborted candidate, even when it was queued behind M.
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(grant.IsClosed807, PreparedLoaderNames807.Closed);
            if (!ReferenceEquals(grant.Issuer807, this) || !preparedLoaderBuilders807.Contains(grant))
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            PreparedBeamImage image = grant.Image807;
            RequirePreparedLoaderImage807(image);
            bool registered = false;
            PreparedLoaderReservation807 reservation;
            if (preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? existing))
                reservation = ReservePreparedLoader807(existing);
            else
            {
                preparedLoaders807.EnsureCapacity(checked(preparedLoaders807.Count + 1));
                preparedLoaderFailures807.EnsureCapacity(checked(preparedLoaderFailures807.Count + preparedLoaders807.Count + 1));
                reservation = ReservePreparedLoader807(candidate);
                try
                {
                    preparedLoaders807.Add(image, candidate);
                    registered = true;
                }
                catch
                {
                    reservation.Dispose();
                    throw;
                }
            }
            grant.PublishReservation807(reservation, registered);
        }
    }

    private void FinishPreparedBuilder807(PreparedLoaderGrant807 grant, Exception? cleanupFailure)
    {
        try
        {
            grant.ReleaseUnclaimed807();
        }
        catch (Exception error)
        {
            cleanupFailure = cleanupFailure is null ? error : new AggregateException(cleanupFailure, error);
        }
        grant.CleanupFailure807 = cleanupFailure;
        if (cleanupFailure is null) grant.PhysicalCompleted807.TrySetResult();
        else grant.PhysicalCompleted807.TrySetException(cleanupFailure);
        if (Monitor.TryEnter(gate))
        {
            try
            {
                CompletePreparedBuilderMetadataCore807(grant);
            }
            catch (Exception error)
            {
                grant.Completed807.TrySetException(error);
            }
            finally
            {
                Monitor.Exit(gate);
            }

            return;
        }
        // There is no native work in this notification. An external retained
        // M cannot prevent factory return/operation release or physical close.
        try
        {
            if (!ThreadPool.UnsafeQueueUserWorkItem(grant, preferLocal: false))
                throw new InvalidOperationException(PreparedLoaderNames807.MetadataQueueFailure);
        }
        catch (Exception error)
        {
            grant.Completed807.TrySetException(error);
        }
    }

    internal void CompletePreparedBuilderMetadata807(PreparedLoaderGrant807 grant)
    {
        try
        {
            lock (gate) CompletePreparedBuilderMetadataCore807(grant);
        }
        catch (Exception error)
        {
            grant.Completed807.TrySetException(error);
        }
    }

    private void CompletePreparedBuilderMetadataCore807(PreparedLoaderGrant807 grant)
    {
        if (!ReferenceEquals(grant.Issuer807, this) || !preparedLoaderBuilders807.Contains(grant))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        if (grant.CleanupFailure807 is null)
        {
            preparedLoaderBuilders807.Remove(grant);
            grant.Completed807.TrySetResult();
        }
        else grant.Completed807.TrySetException(grant.CleanupFailure807);
    }
}
