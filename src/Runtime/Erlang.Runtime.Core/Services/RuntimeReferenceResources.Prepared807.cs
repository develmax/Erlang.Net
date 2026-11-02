namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    private readonly Dictionary<PreparedLoaderOwner807, ReferenceResourceEntry> preparedEntries807 = new(ReferenceEqualityComparer.Instance);

    private ResourceReferenceTerm CreatePreparedNative807(PreparedBeamImage image)
        => CreatePreparedNativeCore807(image, false, out _);

    internal ResourceReferenceTerm CreatePrivatePrepared807(PreparedBeamImage image, out PreparedLoaderOwner807? privateOwner)
        => CreatePreparedNativeCore807(image, true, out privateOwner);

    private ResourceReferenceTerm CreatePreparedNativeCore807(PreparedBeamImage image, bool privateHandle, out PreparedLoaderOwner807? privateOwner)
    {
        privateOwner = null;
        lock (gate) ObjectDisposedException.ThrowIf(closed, ReferenceResourceNames.Registry);
        using PreparedLoaderReservation807 operation = modules.PrepareNativeLoader807(image);
        PreparedLoaderOwner807 owner = operation.Owner807;
        privateOwner = owner;
        ResourceReferenceTerm reference;
        ResourceReferenceTerm? admitted = null;
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closed, ReferenceResourceNames.Registry);
                if (preparedEntries807.TryGetValue(owner, out ReferenceResourceEntry? existing))
                    reference = new ResourceReferenceTerm(existing);
                else
                {
                    preparedEntries807.EnsureCapacity(checked(preparedEntries807.Count + 1));
                    reference = Create(ReferenceResourceKind.PreparedBeam, owner);
                    try
                    {
                        owner.BindIdentity807(reference.Words);
                        preparedEntries807.Add(owner, reference.Entry);
                    }
                    catch
                    {
                        entries.Remove(reference.Entry.Identity);
                        reference.Entry.Close();
                        throw;
                    }
                }
            }
            // The factory owns a real Operation until an actual process header
            // or Host carrier has been installed; registry presence adds none.
            admitted = privateHandle
                ? PreparedLoaderRoots807.HostCopy807(reference)
                : OwnedTermInvocation506.AdmitRehydratedPrepared807(reference, owner);
            owner.CompleteHandoff807();

            return admitted;
        }
        catch (Exception primary)
        {
            // Actual operation/partial-header cleanup remains independent. The
            // raw image producer is released through its handoff-aware boundary.
            Exception? first = null, second = null;
            try
            {
                admitted?.PreparedHost807?.Dispose();
            }
            catch (Exception cleanup)
            {
                first = cleanup;
            }
            try
            {
                image.Dispose();
            }
            catch (Exception cleanup)
            {
                second = cleanup;
            }
            if (first is not null || second is not null)
                throw new AggregateException(new[] { primary, first, second }.OfType<Exception>());
            throw;
        }
    }

    internal PreparedLoaderReservation807? ReservePreparedNative807(ReferenceTerm reference)
    {
        ResourceReferenceTerm resource;
        lock (gate)
        {
            if (closed || reference is not ResourceReferenceTerm selected || !ReferenceEquals(selected.Entry.Owner, this)
                || selected.ResourceKind != ReferenceResourceKind.PreparedBeam || !selected.Entry.IsAlive)
                return null;
            resource = selected;
        }
        try
        {
            PreparedLoaderOwner807 owner = PreparedLoaderRoots807.Direct807(resource)
                ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            lock (gate)
            {
                if (closed || !resource.Entry.IsAlive || !ReferenceEquals(resource.PreparedBacking807, owner)) return null;
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
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal bool ReadPreparedOnLoad807(ReferenceTerm reference)
    {
        using PreparedLoaderReservation807? operation = ReservePreparedNative807(reference);
        if (operation is null) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return operation.Image807.HasOnLoad;
    }

    internal PreparedLoaderWaiting807? ReservePreparedWaiting807(ReferenceTerm reference)
    {
        ResourceReferenceTerm resource;
        lock (gate)
        {
            if (closed || reference is not ResourceReferenceTerm selected || !ReferenceEquals(selected.Entry.Owner, this)
                || selected.ResourceKind != ReferenceResourceKind.PreparedBeam || !selected.Entry.IsAlive)
                return null;
            resource = selected;
        }
        // Existing legacy Host callbacks may already own writer permission.
        // Their portable owning Host handles require no foreign heap read.
        // A physical source header instead requires the real outer seam.
        if (modules.PreparedModuleGateHeld807 && resource.OwnedStorage562 is not null)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        try
        {
            PreparedLoaderOwner807 owner = PreparedLoaderRoots807.Direct807(resource)
                ?? throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            var host = new PreparedLoaderHost807(owner);
            try
            {
                return new(resource.Entry, owner, host);
            }
            catch
            {
                host.Dispose();
                throw;
            }
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    internal PreparedLoaderReservation807 UpgradePreparedWaiting807(PreparedLoaderWaiting807 waiting)
    {
        lock (gate)
        {
            ReferenceResourceEntry entry = waiting.Entry807;
            if (closed || !ReferenceEquals(entry.Owner, this) || entry.Kind != ReferenceResourceKind.PreparedBeam
                || !entry.IsAlive || !waiting.IsHeld807 || !ReferenceEquals(entry.Read<PreparedLoaderOwner807>(), waiting.Owner807))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            try
            {
                PreparedLoaderLease807 lease = waiting.Owner807.Retain807(PreparedLoaderLeaseKind807.Operation);
                try
                {
                    return new(waiting.Owner807, lease);
                }
                catch
                {
                    lease.Dispose();
                    throw;
                }
            }
            catch (ObjectDisposedException)
            {
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }

    private ReferenceResourceLease<PreparedBeamImage>? AcquirePreparedNative807(ReferenceTerm reference)
    {
        using PreparedLoaderReservation807? operation = ReservePreparedNative807(reference);
        if (operation is null) return null;
        var resource = (ResourceReferenceTerm)reference;
        PreparedLoaderOwner807 owner = operation.Owner807;
        PreparedLoaderHost807? host = null;
        try
        {
            if (operation.Image807.IsConsumed) return null;
            host = new PreparedLoaderHost807(owner);
            lock (gate)
            {
                if (closed || !resource.Entry.IsAlive) return null;
                var result = new ReferenceResourceLease<PreparedBeamImage>(resource.Entry, owner, host);
                host = null;

                return result;
            }
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
        finally
        {
            host?.Dispose();
        }
    }

    internal void ForgetPreparedLoader807(PreparedLoaderOwner807 owner)
    {
        lock (gate)
        {
            if (!preparedEntries807.Remove(owner, out ReferenceResourceEntry? entry)) return;
            entries.Remove(entry.Identity);
            entry.Close();
        }
    }
}
