using System.Runtime.ExceptionServices;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal bool PreparedModuleGateHeld807 => Monitor.IsEntered(gate) || CodeModification.IsOwned;

    internal void RequirePreparedShutdownBinding807(PreparedBeamImage image, PreparedLoaderOwner807 owner)
    {
        if (!Monitor.IsEntered(gate) || !preparedLoaderAdmissionClosed807
            || !preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? actual)
            || !ReferenceEquals(actual, owner))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
    }

    internal Term CommitPreparedFinish807(PreparedLoaderFinishPlan807 plan)
    {
        lock (gate) return plan.CommitUnderGate807(this);
    }

    private PreparedLoaderImageReservations807 ReservePreparedImagesUnderGate807(IReadOnlyList<PreparedBeamImage> images)
    {
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        var reservations = new PreparedLoaderImageReservations807(images.Count);
        try
        {
            for (int index = 0; index < images.Count; index++)
                if (preparedLoaders807.TryGetValue(images[index], out PreparedLoaderOwner807? owner))
                    reservations.Set807(index, ReservePreparedLoader807(owner));

            return reservations;
        }
        catch (Exception primary)
        {
            try
            {
                reservations.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private Term FinishPreparedWaiting807(RuntimeReferenceResources resources, IReadOnlyList<ReferenceTerm> references)
    {
        owningRuntime?.RequireModuleLiteralPreparation800();
        PreparedLoaderFinishPlan807 plan = PreparedLoaderFinishPlan807.CaptureReferences807(resources, references);
        Term? result = null;
        Exception? primary = null;
        try
        {
            using var source = EnterSourceMutation();
            using var modification = CodeModification.Enter();
            lock (gate) result = plan.CommitUnderGate807(this);
        }
        catch (Exception error)
        {
            primary = error;
        }
        try
        {
            plan.Dispose();
        }
        catch (Exception cleanup)
        {
            if (primary is not null) throw new AggregateException(primary, cleanup);
            throw;
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();

        return result!;
    }

    internal void RequirePreparedFinishCaller807(ProcessContext context)
    {
        RequireAsyncSourceCaller();
        context.Runtime.ThrowIfHalted();
    }

    internal async Task<PreparedLoaderCloseDisposition807> ResolvePreparedLoaderClose807(
        PreparedBeamImage image,
        PreparedLoaderOwner807 owner
    )
    {
        if (owner.TryReadShutdownClose807(out PreparedLoaderCloseDisposition807? shutdown)) return shutdown!;
        if (Monitor.IsEntered(gate) || CodeModification.IsOwned)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        owner.Runtime807.RequireModuleLiteralPreparation800();
        // Only the ordinary metadata decision can wait for M. Physical cleanup
        // races that decision against the privileged shutdown snapshot, so a
        // direct-M shutdown never waits for its own retained monitor.
        Task<PreparedLoaderCloseDisposition807> ordinary = Task.Run(() => CapturePreparedLoaderClose807(image, owner));
        Task<PreparedLoaderCloseDisposition807> winner = await Task.WhenAny(ordinary, owner.ShutdownClose807).ConfigureAwait(false);

        return await winner.ConfigureAwait(false);
    }

    private PreparedLoaderCloseDisposition807 CapturePreparedLoaderClose807(PreparedBeamImage image, PreparedLoaderOwner807 owner)
    {
        if (owner.TryReadShutdownClose807(out PreparedLoaderCloseDisposition807? shutdown)) return shutdown!;
        lock (gate)
        {
            if (owner.TryReadShutdownClose807(out shutdown)) return shutdown!;
            if (!ReferenceEquals(image.Owner, this)
                || !preparedLoaders807.TryGetValue(image, out PreparedLoaderOwner807? registered)
                || !ReferenceEquals(registered, owner))
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            if (image.Consumed) return new(
                owner,
                image,
                Task.CompletedTask,
                true
            );
            image.Disposed800 = true;

            return new(
                owner,
                image,
                image.LiteralDomain800?.CloseCodeOwner800() ?? Task.CompletedTask,
                false
            );
        }
    }

    private void CapturePreparedLoaderShutdown807(PreparedBeamImage image, PreparedLoaderOwner807 owner)
    {
        if (!Monitor.IsEntered(gate) || !preparedLoaderAdmissionClosed807)
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        bool consumed = image.Consumed;
        Task close = Task.CompletedTask;
        if (!consumed)
        {
            image.Disposed800 = true;
            try
            {
                close = image.LiteralDomain800?.CloseCodeOwner800() ?? Task.CompletedTask;
            }
            catch (Exception error)
            {
                close = Task.FromException(error);
            }
        }
        owner.InstallShutdownClose807(new(
            owner,
            image,
            close,
            consumed
        ));
    }
}
