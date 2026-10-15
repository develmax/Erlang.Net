namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal async ValueTask<NifImageLoadReservation> ReserveNifImageAsync(ProcessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ReferenceEquals(context.Runtime.Modules, this))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ForeignRuntime);
        SourceCodeEntry caller = context.CaptureExecutingCode()
            ?? throw new NifImageAdmissionException(NifImageAdmissionFailure.NoExecutingCaller);
        var permission = await AcquireCodeModificationAsync(context).ConfigureAwait(false);
        SourceCodeLease? lease = null;
        NifImageBindingReservation? binding = null;
        try
        {
            return permission.Run(() =>
            {
                lock (gate)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    var generation = CaptureNifGeneration(context, caller);
                    lease = ReserveSourceCode(context, caller.Image);
                    binding = caller.Image.NifBindings.Begin(generation.TargetDescriptor);

                    return new NifImageLoadReservation(
                        this,
                        permission,
                        lease,
                        binding,
                        generation
                    );
                }
            });
        }
        catch
        {
            try
            {
                binding?.Dispose();
            }
            finally
            {
                try
                {
                    lease?.Dispose();
                }
                finally
                {
                    permission.Dispose();
                }
            }
            throw;
        }
    }

    private NifImageAdmissionGeneration CaptureNifGeneration(ProcessContext context, SourceCodeEntry caller)
    {
        string module = caller.Image.Module;
        if (!context.IsExecutingCode(caller.Image))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.NoExecutingCaller);
        var current = codeImages.GetValueOrDefault(module);
        var previous = oldCode.GetValueOrDefault(module);
        var descriptor = codeDescriptors.GetValueOrDefault(module);
        var pending = pendingBeamImages.GetValueOrDefault(module);
        if (ReferenceEquals(previous?.Image, caller.Image))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.OldCode);
        ModuleCodeDescriptor target;
        ModuleCodeImage? previousNifImage;
        NifImageAdmissionOrigin origin;
        if (ReferenceEquals(current, caller.Image))
        {
            // OTP selects module.on_load after its old-code check. Do not attach a
            // different pending image from a current caller without proving that
            // interaction. This internal coordinator supports exact-image callers.
            if (pending is not null)
                throw new NifImageAdmissionException(NifImageAdmissionFailure.PendingCurrentAmbiguous);
            target = descriptor ?? throw new NifImageAdmissionException(NifImageAdmissionFailure.MissingDescriptor);
            if (!ReferenceEquals(target.SourceOwner, caller.Owner)
                && !functions.Any(entry => entry.Key.Module == module && ReferenceEquals(entry.Value, caller.Owner)))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.OwnerMismatch);
            previousNifImage = previous?.Image;
            origin = NifImageAdmissionOrigin.Current;
        }
        else if (pending is not null && ReferenceEquals(pending.CodeImage, caller.Image)
            && ReferenceEquals(pending, caller.Owner))
        {
            if (pending.CurrentPhase is not (BeamOnLoadPhase.Staged or BeamOnLoadPhase.Running))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.UnknownImage);
            target = pending.Prepared.Descriptor;
            previousNifImage = current ?? previous?.Image;
            origin = NifImageAdmissionOrigin.PendingOnLoad;
        }
        else
        {
            throw new NifImageAdmissionException(NifImageAdmissionFailure.UnknownImage);
        }
        if (NifImageBindingStore.IsCreated(caller.Image) && caller.Image.NifBindings.Phase == NifImageBindingPhase.Ready)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.Reload);
        if (previousNifImage is not null && NifImageBindingStore.IsCreated(previousNifImage)
            && previousNifImage.NifBindings.Phase == NifImageBindingPhase.Ready)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.UpgradeUnsupported);

        return new(
            context,
            caller,
            origin,
            target,
            current,
            previous?.Image,
            descriptor,
            pending,
            functions.Where(entry => entry.Key.Module == module).ToDictionary(entry => entry.Key, entry => entry.Value),
            sourceOwners.Where(entry => entry.Key.Module == module).ToDictionary(entry => entry.Key, entry => entry.Value)
        );
    }

    internal void CommitNifImage(
        NifImageLoadReservation reservation,
        NifImageBindingReservation binding,
        KeyValuePair<(string Name, int Arity), ErlangFunction>[] bindings,
        IAsyncDisposable owner
    )
    {
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            var generation = reservation.Generation;
            string module = generation.Caller.Image.Module;
            var pending = pendingBeamImages.GetValueOrDefault(module);
            if (!ReferenceEquals(codeImages.GetValueOrDefault(module), generation.CurrentImage)
                || !ReferenceEquals(oldCode.GetValueOrDefault(module)?.Image, generation.OldImage)
                || !ReferenceEquals(codeDescriptors.GetValueOrDefault(module), generation.CurrentDescriptor)
                || !ReferenceEquals(pending, generation.PendingTicket)
                || !MatchesNifBodies(module, generation))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.GenerationChanged);
            if (generation.Origin == NifImageAdmissionOrigin.PendingOnLoad
                && (pending is null || !ReferenceEquals(pending.CodeImage, reservation.Image)
                    || !ReferenceEquals(pending.Prepared.Descriptor, generation.TargetDescriptor)
                    || pending.CurrentPhase is not (BeamOnLoadPhase.Staged or BeamOnLoadPhase.Running)))
                throw new NifImageAdmissionException(NifImageAdmissionFailure.GenerationChanged);
            generation.Context.Runtime.ThrowIfHalted();
            generation.Context.Cancellation.ThrowIfCancellationRequested();
            reservation.BeginCommit();
            binding.Publish(bindings, owner);
            if (generation.Origin == NifImageAdmissionOrigin.Current)
                informationSnapshots.Remove(module);
        }
    }

    private bool MatchesNifBodies(string module, NifImageAdmissionGeneration generation)
    {
        var bodies = functions.Where(entry => entry.Key.Module == module).ToArray();
        var owners = sourceOwners.Where(entry => entry.Key.Module == module).ToArray();

        return bodies.Length == generation.Bodies.Count && owners.Length == generation.SourceOwners.Count
            && bodies.All(entry => generation.Bodies.TryGetValue(entry.Key, out var body) && ReferenceEquals(body, entry.Value))
            && owners.All(entry => generation.SourceOwners.TryGetValue(entry.Key, out var owner) && ReferenceEquals(owner, entry.Value));
    }
}
