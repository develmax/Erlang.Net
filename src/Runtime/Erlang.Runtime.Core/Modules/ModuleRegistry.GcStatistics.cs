namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly object gcStatisticsGate = new();
    private readonly TaskCompletionSource gcStatisticsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<PreparedBeamImage>? gcStatisticsPreparation;
    private PreparedBeamImage? gcStatisticsPrivateImage;
    private bool gcStatisticsPreparing, gcStatisticsClosing;
    private int gcStatisticsUses;

    internal async ValueTask<GcStatisticsAdmission> AdmitGcStatistics(ProcessContext caller)
    {
        if (!ReferenceEquals(owningRuntime, caller.Runtime) || !RuntimeActorExecutionTurn.IsOwnedBy(caller))
            throw new InvalidOperationException(GcStatisticsNames.Invalid);
        caller.Cancellation.ThrowIfCancellationRequested();
        PreparedBeamImage? selected;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(sourceClosing || nifClosing, this);
            selected = sourceOwners.GetValueOrDefault((GcStatisticsNames.Module, GcStatisticsNames.Helper, GcStatisticsNames.Arity)) as PreparedBeamImage;
            if (selected is not null && (!ReferenceEquals(selected.Owner, this)
                || selected.OriginalBeamSha256 != GcStatisticsNames.ImageSha256
                || !ReferenceEquals(codeImages.GetValueOrDefault(GcStatisticsNames.Module), selected.ExecutionIdentity.Image)))
                selected = null;
        }
        selected ??= await PreparePrivateGcStatistics(caller);
        SourceCodeLease lease = await AcquireSourceEntry(caller, selected.ExecutionIdentity.Image, SourceCodeAdmissionKind.Function);
        try
        {
            lock (gcStatisticsGate)
            {
                ObjectDisposedException.ThrowIf(gcStatisticsClosing || Volatile.Read(ref sourceClosing) || Volatile.Read(ref nifClosing), this);
                var admission = new GcStatisticsAdmission(
                    this,
                    caller,
                    selected,
                    lease
                );
                checked
                {
                    gcStatisticsUses++;
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

    private async ValueTask<PreparedBeamImage> PreparePrivateGcStatistics(ProcessContext caller)
    {
        TaskCompletionSource<PreparedBeamImage> preparation;
        bool prepare = false;
        lock (gcStatisticsGate)
        {
            ObjectDisposedException.ThrowIf(gcStatisticsClosing || Volatile.Read(ref sourceClosing) || Volatile.Read(ref nifClosing), this);
            if (gcStatisticsPreparation is null)
            {
                gcStatisticsPreparation = new(TaskCreationOptions.RunContinuationsAsynchronously);
                gcStatisticsPreparing = true;
                prepare = true;
            }
            preparation = gcStatisticsPreparation;
        }
        if (prepare)
            _ = ProduceGcStatisticsImage(preparation);

        return await preparation.Task.WaitAsync(caller.Cancellation);
    }

    private async Task ProduceGcStatisticsImage(TaskCompletionSource<PreparedBeamImage> preparation)
    {
        PreparedBeamImage? candidate = null;
        try
        {
            candidate = await PrepareGcStatisticsImage().ConfigureAwait(false);
            if (candidate.Name != GcStatisticsNames.Module || candidate.HasOnLoad
                || candidate.OriginalBeamSha256 != GcStatisticsNames.ImageSha256)
                throw new InvalidDataException(GcStatisticsNames.Invalid);
            gcStatisticsPrivateImage = candidate;
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
            lock (gcStatisticsGate)
            {
                gcStatisticsPreparing = false;
                CompleteGcStatisticsDrain();
            }
        }
    }

    private async ValueTask<PreparedBeamImage> PrepareGcStatisticsImage()
    {
        RequireAsyncSourceCaller();
        // The shared VM preparation has its own source admission. Each caller
        // may cancel its wait, while registry shutdown cancels the preparation.
        using ModuleSourceMutationTicket ticket = await SourceMutation.AcquireAsync(CancellationToken.None).ConfigureAwait(false);
        // This scope is thread-local: no await may occur before its disposal.
        // Preparing literal arenas intentionally runs outside the code writer.
        using var source = new ModuleSourceMutationScope(ticket, false);
        ThrowIfSourceClosing();

        return PrepareBeam(GcStatisticsResource.AuthenticatedImage(), new BeamDependencySelection(GcStatisticsImageImports.Modules));
    }

    internal ValueTask<Term> InvokeGcStatistics(ProcessContext caller, GcStatisticsAdmission admission, ReferenceTerm reference)
    {
        PreparedBeamImage image = admission.Image;
        if (!ReferenceEquals(image.Owner, this) || !ReferenceEquals(owningRuntime, caller.Runtime)
            || !RuntimeActorExecutionTurn.IsOwnedBy(caller) || image.OriginalBeamSha256 != GcStatisticsNames.ImageSha256
            || !admission.TryStart(this, caller))
            throw new InvalidOperationException(GcStatisticsNames.Invalid);
        BeamLinkedFunction function = image.Code.Functions.Single(value => value.Name == GcStatisticsNames.Helper && value.Arity == GcStatisticsNames.Arity);
        ErlangFunction body = (context, arguments) => BeamRegisterInterpreter.ExecuteFunction(
            context,
            image.Code,
            function,
            arguments,
            Array.Empty<Term>(),
            image.ExecutionIdentity,
            admittedLease647: admission.Lease
        );
        // A runtime helper does not enroll an opaque C# Host stack in physical
        // guest collection. Existing guest contexts retain their real roots.
        OwnedBindingKind507 kind = caller.OwnedExecutionEnabled683 ? OwnedBindingKind507.PreparedGuest : OwnedBindingKind507.Opaque;

        return InvokeSelected(
            caller,
            GcStatisticsNames.Module,
            GcStatisticsNames.Helper,
            [reference],
            body,
            null,
            null,
            kind
        );
    }

    internal void ReleaseGcStatisticsUse()
    {
        lock (gcStatisticsGate)
        {
            if (gcStatisticsUses <= 0) throw new InvalidOperationException(GcStatisticsNames.Invalid);
            gcStatisticsUses--;
            CompleteGcStatisticsDrain();
        }
    }

    private void CompleteGcStatisticsDrain()
    {
        if (gcStatisticsClosing && !gcStatisticsPreparing && gcStatisticsUses == 0)
            gcStatisticsDrained.TrySetResult();
    }

    private async Task CloseGcStatisticsHelper()
    {
        Task drained;
        lock (gcStatisticsGate)
        {
            gcStatisticsClosing = true;
            CompleteGcStatisticsDrain();
            drained = gcStatisticsDrained.Task;
        }
        await drained.ConfigureAwait(false);
        if (gcStatisticsPrivateImage is { } image)
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
