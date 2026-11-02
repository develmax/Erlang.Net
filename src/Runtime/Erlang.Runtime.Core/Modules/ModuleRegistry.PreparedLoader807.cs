namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Identity/drain bookkeeping only: this dictionary never adds a native ref.
    private readonly Dictionary<PreparedBeamImage, PreparedLoaderOwner807> preparedLoaders807 = new(ReferenceEqualityComparer.Instance);
    private readonly List<Exception> preparedLoaderFailures807 = new();
    private bool preparedLoaderAdmissionClosed807;
    private readonly HashSet<PreparedLoaderGrant807> preparedLoaderBuilders807 = new(ReferenceEqualityComparer.Instance);

    internal PreparedLoaderReservation807 PrepareNativeLoader807(PreparedBeamImage image)
    {
        if (Monitor.IsEntered(gate) || CodeModification.IsOwned)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        ProcessRuntime runtime = owningRuntime ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        runtime.RequireModuleLiteralPreparation800();
        lock (gate)
        {
            RequirePreparedLoaderImage807(image);
            if (preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? existing))
                return ReservePreparedLoader807(existing);
        }
        PreparedLoaderGrant807 grant = IssuePreparedLoaderGrant807(image);
        PreparedLoaderOwner807? candidate = null;
        PreparedLoaderReservation807? reservation = null;
        bool registered = false;
        Exception? cleanupFailure = null;
        try
        {
            candidate = new PreparedLoaderOwner807(
                this,
                runtime,
                image,
                grant
            );
            reservation = CompletePreparedLoaderRegistration807(grant, candidate);
            registered = grant.IsRegistered807;
            if (!registered) candidate.AbortUnregistered807();

            return reservation!;
        }
        catch (Exception primary)
        {
            Exception? first = null, second = null;
            registered = grant.IsRegistered807;
            reservation ??= grant.TakeReservation807();
            try
            {
                reservation?.Dispose();
            }
            catch (Exception error)
            {
                first = error;
            }
            try
            {
                if (candidate is not null)
                {
                    if (registered) candidate.ReleaseProducer807();
                    else candidate.AbortUnregistered807();
                }
            }
            catch (Exception error)
            {
                second = error;
            }
            if (first is not null || second is not null)
            {
                cleanupFailure = new AggregateException(new[] { primary, first, second }.OfType<Exception>());
                grant.FailedCandidate807 = !registered ? candidate : null;
                throw cleanupFailure;
            }
            throw;
        }
        finally
        {
            FinishPreparedBuilder807(grant, cleanupFailure);
        }
    }

    internal ModuleLiteralLease800? ClaimPreparedLoaderLiteral807(PreparedLoaderGrant807 grant, PreparedBeamImage image)
        => grant.Claim807(this, image);

    internal void PreparedLoaderImageConsumed807(PreparedBeamImage image)
    {
        if (!Monitor.IsEntered(gate) || !image.Consumed)
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        if (preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? owner))
            owner.ReleasePreparedLiterals807(image);
    }

    private void RequirePreparedLoaderImage807(PreparedBeamImage image)
    {
        ThrowIfSourceClosing();
        ThrowIfNifClosing();
        ObjectDisposedException.ThrowIf(preparedLoaderAdmissionClosed807, PreparedLoaderNames807.Closed);
        if (!ReferenceEquals(image.Owner, this) || image.Consumed || image.Disposed800)
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
    }

    private static PreparedLoaderReservation807 ReservePreparedLoader807(PreparedLoaderOwner807 owner)
    {
        PreparedLoaderLease807 lease = owner.Retain807(PreparedLoaderLeaseKind807.Operation);
        try
        {
            return new(owner, lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    // Used by direct image publication/disposal after native handoff. The
    // caller retains its original module validation/Consumed serialization.
    internal bool TryReservePreparedLoader807(PreparedBeamImage image, out PreparedLoaderReservation807? reservation)
    {
        lock (gate)
        {
            if (!ReferenceEquals(image.Owner, this)) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            reservation = preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? owner)
                ? ReservePreparedLoader807(owner) : null;

            return reservation is not null;
        }
    }

    internal bool TryReleasePreparedProducer807(PreparedBeamImage image)
    {
        lock (gate)
        {
            if (!ReferenceEquals(image.Owner, this)) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            if (!preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? owner)) return false;
            owner.ReleaseProducer807();

            return true;
        }
    }

    internal void PreparedLoaderSettled807(
        PreparedBeamImage? image,
        PreparedLoaderOwner807 owner,
        bool freed,
        Exception? failure
    )
    {
        if (freed) owner.Runtime807.ReferenceResources.ForgetPreparedLoader807(owner);
        lock (gate)
        {
            image ??= preparedLoaders807.FirstOrDefault(pair => ReferenceEquals(pair.Value, owner)).Key;
            if (image is null || !preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? actual)
                || !ReferenceEquals(actual, owner))
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            if (!freed) return; // The actual failed native owner remains visible.
            if (failure is not null) preparedLoaderFailures807.Add(failure);
            preparedLoaders807.Remove(image);
        }
    }

    // This method only snapshots metadata under M. Actual close/Join calls
    // happen after M is released; lifecycle integration remains mandatory.
    internal Task[] ClosePreparedLoaderOwners807(bool direct)
    {
        KeyValuePair<PreparedBeamImage, PreparedLoaderOwner807>[] owners;
        Exception[] failures;
        Task[] builders;
        lock (gate)
        {
            preparedLoaderAdmissionClosed807 = true;
            owners = preparedLoaders807.ToArray();
            failures = preparedLoaderFailures807.ToArray();
            foreach (PreparedLoaderGrant807 grant in preparedLoaderBuilders807) grant.CloseAdmission807();
            builders = preparedLoaderBuilders807.Select(grant => direct
                ? grant.PhysicalCompleted807.Task : grant.Completed807.Task).ToArray();
            foreach (var entry in owners) CapturePreparedLoaderShutdown807(entry.Key, entry.Value);
        }
        foreach (var entry in owners) entry.Value.CloseVm807();

        return owners.Select(entry => direct ? entry.Value.WaitPhysical807 : entry.Value.Join807())
            .Concat(builders).Concat(failures.Select(Task.FromException)).ToArray();
    }
}
