namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal async ValueTask<SourcePurgeFenceSession?> PreparePurgeAsync432(ProcessContext context, string module, bool onLoad)
    {
        if (!onLoad) return await PreparePurgeAsync(context, module).ConfigureAwait(false);
        RequireAsyncSourceCaller();
        context.Runtime.ThrowIfHalted();
        ModuleSourceMutationTicket? permission = null;
        bool transferred = false;
        try
        {
            permission = await AcquireSourceMutationAsync(context).ConfigureAwait(false);
            ModuleSourceMutationTicket owned = permission;
            using var writer = await AcquireCodeModificationAsync(context).ConfigureAwait(false);
            SourcePurgeFenceSession? session = writer.Run(() =>
            {
                using var scope = new ModuleSourceMutationScope(owned, false);
                context.Cancellation.ThrowIfCancellationRequested();

                return PrepareOnLoadPurgeCore432(module, owned);
            });
            transferred = session is not null;

            return session;
        }
        catch (ModuleSourceMutationAdmissionException error) when (error.Failure == ModuleSourceMutationFailure.RuntimeClosing && ReferenceEquals(error.Registry, this))
        {
            throw new ModuleSourceMutationClosedException(this, context.Cancellation);
        }
        finally
        {
            if (!transferred) permission?.Dispose();
        }
    }

    private SourcePurgeFenceSession? PrepareOnLoadPurgeCore432(string module, ModuleSourceMutationTicket permission)
    {
        lock (gate)
        {
            ThrowIfSourceClosing();
            if (!pendingBeamImages.TryGetValue(module, out BeamOnLoadTicket? pending))
            {
                if (!knownModules.Contains(module)) return null;
                // Native asserts a real on_load image on this privileged source path.
                throw new InvalidOperationException(CodePurgerNames432.OnLoadState);
            }
            if (!loaderPendingImages.TryGetValue(module, out var loader) || !ReferenceEquals(loader, pending) || pending.HasDeferredExports)
                throw new InvalidOperationException(CodePurgerNames432.OnLoadState);
            ModuleCodeImage image = pending.CodeImage;
            var state = purgeStates.GetOrCreateValue(image);
            if (state.Pending is not null || sourcePurgeSessions.ContainsKey(image))
                throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.InvalidSession);
            // Allocate all graph/collections before changing old/pending code ownership.
            var entries = pending.Prepared.Code.Exports.Select((function, index) => (
                (module, function.Name, function.Arity), pending.Prepared.Bodies[index])).ToArray();
            var snapshot = new ModuleCodeSnapshot(
                Array.AsReadOnly(entries),
                pending.Prepared.Descriptor,
                image,
                Array.AsReadOnly(new object[] { pending.Prepared.ExecutionIdentity.Owner })
            );
            var session = new SourcePurgeFenceSession(
                this,
                module,
                image,
                permission
            )
            { OnLoad432 = new(pending, oldCode.GetValueOrDefault(module)) };
            oldCode.EnsureCapacity(checked(oldCode.Count + 1));
            sourcePurgeSessions.EnsureCapacity(checked(sourcePurgeSessions.Count + 1));
            permission.TransferToSession();
            sourcePurgeSessions.Add(image, session);
            oldCode[module] = snapshot;
            loaderPendingImages.Remove(module);
            pendingBeamImages.Remove(module);
            state.Session = session;
            state.Pending = session.Pending;

            return session;
        }
    }

    // Existing registry callers already hold gate. A saved image remains a real
    // code/FUN/NIF owner while only the rejected on_load image occupies oldCode.
    private ModuleCodeSnapshot? SavedOldCode432(string module)
    {
        if (sourcePurgeSessions.Count == 0) return null;
        foreach (SourcePurgeFenceSession session in sourcePurgeSessions.Values)
            if (session.Module == module && session.OnLoad432 is { } rejected) return rejected.SavedOld;

        return null;
    }

    private IEnumerable<ModuleCodeImage> SavedOldImages432()
        => sourcePurgeSessions.Values.Select(session => session.OnLoad432?.SavedOld?.Image).OfType<ModuleCodeImage>();

    private (ISourceLocalFunctionResolver Resolver, ModuleCodeImage Image)? SavedOldFunction432(LocalFunctionIdentity identity)
    {
        ModuleCodeSnapshot? saved = SavedOldCode432(identity.Module.Name);
        if (saved?.Descriptor is not { } descriptor || !descriptor.Fingerprint.Equals(identity.Unique)) return null;
        if (saved.Image.ExecutionOwner is PreparedBeamImage prepared && ReferenceEquals(prepared.Owner, this)
            && ReferenceEquals(prepared.ExecutionIdentity.Image, saved.Image) && saved.SourceOwners.Any(owner => ReferenceEquals(owner, prepared)))
            return (prepared, saved.Image);
        if (descriptor.SourceOwner is ISourceLocalFunctionResolver resolver && saved.SourceOwners.Any(owner => ReferenceEquals(owner, resolver)))
            return (resolver, saved.Image);

        return null;
    }

    private void RestoreSavedOld432(SourcePurgeFenceSession session)
    {
        if (session.OnLoad432 is not { } rejected) return;
        if (rejected.SavedOld is { } saved) oldCode[session.Module] = saved;
        rejected.Ticket.CurrentPhase = BeamOnLoadPhase.Retired;
        if (rejected.Ticket.CallbackOwner is null) rejected.Ticket.CallbackUnwound.TrySetResult();
        session.OnLoad432 = null;
    }
}
