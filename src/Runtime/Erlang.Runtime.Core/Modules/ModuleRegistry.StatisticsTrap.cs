namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly object statisticsTrapGate = new();
    private readonly TaskCompletionSource statisticsTrapDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<PreparedBeamImage>? statisticsTrapPreparation;
    private PreparedBeamImage? statisticsTrapPrivateImage;
    private bool statisticsTrapPreparing, statisticsTrapClosing;
    private int statisticsTrapUses;

    internal void PrewarmStatisticsTrapHelpers(ProcessContext initializer)
    {
        if (System.Threading.Monitor.IsEntered(gate) || !ReferenceEquals(owningRuntime, initializer.Runtime)
            || RuntimeActorExecutionTurn.IsOwnedBy(initializer))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        _ = PreparePrivateStatisticsTrap(initializer).GetAwaiter().GetResult();
    }

    private static void RequireStatisticsTrapSignature(string function, int arity)
    {
        bool allowed = (function, arity) is (StatisticsTrapNames.AwaitMicrostates, 3)
            or (StatisticsTrapNames.GatherMicrostates, 2)
            or (StatisticsTrapNames.AwaitWallTime, 2)
            or (StatisticsTrapNames.GatherWallTime, 1);
        if (!allowed) throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
    }

    internal async ValueTask<StatisticsTrapAdmission> AdmitStatisticsTrap(ProcessContext caller, string function, int arity)
    {
        RequireStatisticsTrapSignature(function, arity);
        if (!ReferenceEquals(owningRuntime, caller.Runtime) || !RuntimeActorExecutionTurn.IsOwnedBy(caller))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        caller.Cancellation.ThrowIfCancellationRequested();
        PreparedBeamImage? selected;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(sourceClosing || nifClosing, this);
            selected = sourceOwners.GetValueOrDefault((StatisticsTrapNames.Module, function, arity)) as PreparedBeamImage;
            if (selected is not null && (!ReferenceEquals(selected.Owner, this)
                || selected.OriginalBeamSha256 != StatisticsTrapNames.ImageSha256
                || !ReferenceEquals(codeImages.GetValueOrDefault(StatisticsTrapNames.Module), selected.ExecutionIdentity.Image)))
                selected = null;
        }
        selected ??= await PreparePrivateStatisticsTrap(caller);
        SourceCodeLease lease = await AcquireSourceEntry(caller, selected.ExecutionIdentity.Image, SourceCodeAdmissionKind.Function);
        try
        {
            lock (statisticsTrapGate)
            {
                ObjectDisposedException.ThrowIf(statisticsTrapClosing || Volatile.Read(ref sourceClosing) || Volatile.Read(ref nifClosing), this);
                var admission = new StatisticsTrapAdmission(
                    this,
                    caller,
                    selected,
                    lease,
                    function,
                    arity
                );
                checked
                {
                    statisticsTrapUses++;
                }

                return admission;
            }
        }
        catch (Exception primary)
        {
            try
            {
                lease.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private async ValueTask<PreparedBeamImage> PreparePrivateStatisticsTrap(ProcessContext caller)
    {
        TaskCompletionSource<PreparedBeamImage> preparation;
        bool prepare = false;
        lock (statisticsTrapGate)
        {
            ObjectDisposedException.ThrowIf(statisticsTrapClosing || Volatile.Read(ref sourceClosing) || Volatile.Read(ref nifClosing), this);
            if (statisticsTrapPreparation is null)
            {
                statisticsTrapPreparation = new(TaskCreationOptions.RunContinuationsAsynchronously);
                statisticsTrapPreparing = true;
                prepare = true;
            }
            preparation = statisticsTrapPreparation;
        }
        if (prepare)
            _ = ProduceStatisticsTrapImage(preparation);

        return await preparation.Task.WaitAsync(caller.Cancellation);
    }

    private async Task ProduceStatisticsTrapImage(TaskCompletionSource<PreparedBeamImage> preparation)
    {
        PreparedBeamImage? candidate = null;
        try
        {
            candidate = await PrepareStatisticsTrapImage().ConfigureAwait(false);
            if (candidate.Name != StatisticsTrapNames.Module || candidate.HasOnLoad
                || candidate.OriginalBeamSha256 != StatisticsTrapNames.ImageSha256)
                throw new InvalidDataException(StatisticsTrapNames.InvalidImage);
            statisticsTrapPrivateImage = candidate;
            preparation.TrySetResult(candidate);
        }
        catch (Exception primary)
        {
            Exception failure = primary;
            if (candidate is not null)
                try
                {
                    await candidate.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    failure = new AggregateException(primary, cleanup);
                }
            preparation.TrySetException(failure);
            // The last waiter may already have cancelled. Preserve the shared
            // failure without leaving an unobserved task exception.
            _ = preparation.Task.Exception;
        }
        finally
        {
            lock (statisticsTrapGate)
            {
                statisticsTrapPreparing = false;
                CompleteStatisticsTrapDrain();
            }
        }
    }

    private async ValueTask<PreparedBeamImage> PrepareStatisticsTrapImage()
    {
        RequireAsyncSourceCaller();
        // The shared VM preparation has its own source admission. Each caller
        // may cancel its wait, while registry shutdown cancels the preparation.
        using ModuleSourceMutationTicket ticket = await SourceMutation.AcquireAsync(CancellationToken.None).ConfigureAwait(false);
        // This scope is thread-local: no await may occur before its disposal.
        // Preparing literal arenas intentionally runs outside the code writer.
        using var source = new ModuleSourceMutationScope(ticket, false);
        ThrowIfSourceClosing();

        return PrepareBeam(StatisticsTrapResource.AuthenticatedImage(), new BeamDependencySelection(StatisticsTrapImageImports.Modules));
    }

    internal ValueTask<Term> InvokeStatisticsTrap(
        ProcessContext caller,
        StatisticsTrapAdmission admission,
        string function,
        params Term[] arguments
    )
    {
        RequireStatisticsTrapSignature(function, arguments.Length);
        PreparedBeamImage image = admission.Image;
        if (!ReferenceEquals(image.Owner, this) || !ReferenceEquals(owningRuntime, caller.Runtime)
            || !RuntimeActorExecutionTurn.IsOwnedBy(caller) || image.OriginalBeamSha256 != StatisticsTrapNames.ImageSha256
            || !admission.TryStart(
                this,
                caller,
                function,
                arguments.Length
            ))
            throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
        BeamLinkedFunction target = image.Code.Functions.Single(value => value.Name == function && value.Arity == arguments.Length);
        ErlangFunction body = (context, values) => BeamRegisterInterpreter.ExecuteFunction(
            context,
            image.Code,
            target,
            values,
            Array.Empty<Term>(),
            image.ExecutionIdentity,
            admittedLease647: admission.Lease
        );
        OwnedBindingKind507 kind = caller.OwnedExecutionEnabled683 ? OwnedBindingKind507.PreparedGuest : OwnedBindingKind507.Opaque;

        return InvokeSelected(
            caller,
            StatisticsTrapNames.Module,
            function,
            arguments,
            body,
            null,
            null,
            kind
        );
    }

    internal void ReleaseStatisticsTrapUse()
    {
        lock (statisticsTrapGate)
        {
            if (statisticsTrapUses <= 0) throw new InvalidOperationException(StatisticsTrapNames.InvalidImage);
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
        Task drained;
        lock (statisticsTrapGate)
        {
            statisticsTrapClosing = true;
            CompleteStatisticsTrapDrain();
            drained = statisticsTrapDrained.Task;
        }
        await drained.ConfigureAwait(false);
        if (statisticsTrapPrivateImage is { } image)
        {
            Exception? retirementFailure = null;
            Exception? literalFailure = null;
            try
            {
                lock (gate) purgeStates.GetOrCreateValue(image.ExecutionIdentity.Image).Retired = true;
                await RetireNifImageAsync(image.ExecutionIdentity.Image).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                retirementFailure = error;
            }
            try
            {
                await image.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                literalFailure = error;
            }
            if (retirementFailure is not null && literalFailure is not null)
                throw new AggregateException(retirementFailure, literalFailure);
            if (retirementFailure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(retirementFailure).Throw();
            if (literalFailure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(literalFailure).Throw();
        }
    }
}
