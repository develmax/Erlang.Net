namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly object statisticsTrapGate = new();
    private TaskCompletionSource<PreparedBeamImage>? statisticsTrapPreparation;
    private readonly TaskCompletionSource statisticsTrapDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private PreparedBeamImage? statisticsTrapPrivateImage;
    private bool statisticsTrapPreparing, statisticsTrapClosing;
    private int statisticsTrapUses;

    internal void PrewarmStatisticsTrapHelpers(ProcessContext initializer)
    {
        if (System.Threading.Monitor.IsEntered(gate) || !ReferenceEquals(owningRuntime, initializer.Runtime))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        _ = PreparePrivateStatisticsTrap(initializer).GetAwaiter().GetResult();
    }

    private static void RequireStatisticsTrapSignature(string function, int arity)
    {
        bool allowed = (function, arity) is (StatisticsTrapNames.AwaitMicrostates, 3)
            or (StatisticsTrapNames.GatherMicrostates, 2)
            or (StatisticsTrapNames.AwaitWallTime, 2)
            or (StatisticsTrapNames.GatherWallTime, 1);
        if (!allowed)
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
    }

    internal async ValueTask<StatisticsTrapAdmission> AdmitStatisticsTrap(ProcessContext caller, string function, int arity)
    {
        RequireStatisticsTrapSignature(function, arity);
        if (!ReferenceEquals(owningRuntime, caller.Runtime))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        caller.Cancellation.ThrowIfCancellationRequested();
        PreparedBeamImage? selected;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(sourceClosing, this);
            selected = sourceOwners.GetValueOrDefault((StatisticsTrapNames.Module, function, arity)) as PreparedBeamImage;
            if (selected is not null && (!ReferenceEquals(selected.Owner, this)
                || selected.OriginalBeamSha256 != StatisticsTrapNames.ImageSha256
                || !ReferenceEquals(codeImages.GetValueOrDefault(StatisticsTrapNames.Module), selected.ExecutionIdentity.Image)))
                selected = null;
        }
        selected ??= await PreparePrivateStatisticsTrap(caller);
        lock (statisticsTrapGate)
        {
            ObjectDisposedException.ThrowIf(statisticsTrapClosing || Volatile.Read(ref sourceClosing), this);
            checked
            {
                statisticsTrapUses++;
            }

            return new StatisticsTrapAdmission(this, selected);
        }
    }

    private async ValueTask<PreparedBeamImage> PreparePrivateStatisticsTrap(ProcessContext caller)
    {
        TaskCompletionSource<PreparedBeamImage> preparing;
        bool ownPreparation = false;
        lock (statisticsTrapGate)
        {
            ObjectDisposedException.ThrowIf(statisticsTrapClosing || Volatile.Read(ref sourceClosing), this);
            if (statisticsTrapPreparation is null)
            {
                statisticsTrapPreparation = new(TaskCreationOptions.RunContinuationsAsynchronously);
                statisticsTrapPreparing = true;
                ownPreparation = true;
            }
            preparing = statisticsTrapPreparation;
        }
        if (ownPreparation)
            try
            {
                // Private preparation is not registry publication or a code-writer
                // transaction. Its actual preparation is owned by this cache's
                // closing/preparing ledger; execution uses normal source leases.
                PreparedBeamImage image = PrepareBeam(StatisticsTrapResource.AuthenticatedImage(),
                    new BeamDependencySelection(StatisticsTrapImageImports.Modules));
                if (image.Name != StatisticsTrapNames.Module || image.HasOnLoad
                    || image.OriginalBeamSha256 != StatisticsTrapNames.ImageSha256)
                    throw new InvalidDataException(StatisticsTrapNames.InvalidImage);
                statisticsTrapPrivateImage = image;
                preparing.TrySetResult(image);
            }
            catch (Exception error)
            {
                preparing.TrySetException(error);
            }
            finally
            {
                lock (statisticsTrapGate)
                {
                    statisticsTrapPreparing = false;
                    CompleteStatisticsTrapDrain();
                }
            }

        // No R/D/M gate across actual source preparation or waiting.
        return await preparing.Task.WaitAsync(caller.Cancellation);
    }

    internal ValueTask<Term> InvokeStatisticsTrap(
        ProcessContext context,
        StatisticsTrapAdmission admission,
        string function,
        params Term[] arguments
    )
    {
        RequireStatisticsTrapSignature(function, arguments.Length);
        PreparedBeamImage image = admission.Image;
        if (!admission.IsCurrentFor(this) || !ReferenceEquals(image.Owner, this) || !ReferenceEquals(owningRuntime, context.Runtime)
            || !RuntimeActorExecutionTurn.IsOwnedBy(context)
            || image.OriginalBeamSha256 != StatisticsTrapNames.ImageSha256)
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        BeamLinkedFunction target = image.Code.Functions.Single(value => value.Name == function && value.Arity == arguments.Length);
        ErlangFunction body = (caller, values) => BeamRegisterInterpreter.ExecuteLocal(
            caller,
            image.Code,
            target,
            values,
            Array.Empty<Term>(),
            image.ExecutionIdentity
        );

        // Exact authenticated prepared guest authority; no public binding is changed.
        // The interpreter independently acquires its normal source entry/purge lease.
        return InvokeSelected(
            context,
            StatisticsTrapNames.Module,
            function,
            arguments,
            body,
            null,
            null,
            OwnedBindingKind507.PreparedGuest
        );
    }

    internal void ReleaseStatisticsTrapUse()
    {
        lock (statisticsTrapGate)
        {
            if (statisticsTrapUses <= 0)
                throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
            statisticsTrapUses--;
            CompleteStatisticsTrapDrain();
        }
    }

    private void CompleteStatisticsTrapDrain()
    {
        if (statisticsTrapClosing && !statisticsTrapPreparing && statisticsTrapUses == 0)
            statisticsTrapDrained.TrySetResult();
    }

    private async Task CloseStatisticsTrapHelpers()
    {
        Task drain;
        lock (statisticsTrapGate)
        {
            statisticsTrapClosing = true;
            CompleteStatisticsTrapDrain();
            drain = statisticsTrapDrained.Task;
        }
        await drain.ConfigureAwait(false);
        PreparedBeamImage? image = statisticsTrapPrivateImage;
        if (image is not null)
        {
            lock (gate)
                purgeStates.GetOrCreateValue(image.ExecutionIdentity.Image).Retired = true;
            if (NifImageBindingStore.TryGetValue(image.ExecutionIdentity.Image, out var native))
                await native!.RetireAsync().ConfigureAwait(false);
        }
    }
}
