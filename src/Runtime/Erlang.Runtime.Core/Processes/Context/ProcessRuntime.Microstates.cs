namespace Erlang;

public sealed partial class ProcessRuntime
{
    private ulong nextMicrostateCapture1171;

    internal ValueTask<Term> SetMicrostates(ProcessContext context, Term option)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
        RuntimeMicrostateAction action = option switch
        {
            Atom { Name: ErlangBooleanAtoms.True } => RuntimeMicrostateAction.Enable,
            Atom { Name: ErlangBooleanAtoms.False } => RuntimeMicrostateAction.Disable,
            Atom { Name: RuntimeMicrostateNames.Reset } => RuntimeMicrostateAction.Reset,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        return RequestMicrostates(context, action);
    }

    internal ValueTask<Term> SampleMicrostates(ProcessContext context)
        => RequestMicrostates(context, RuntimeMicrostateAction.Gather);

    private async ValueTask<Term> RequestMicrostates(ProcessContext context, RuntimeMicrostateAction action)
    {
        while (true)
        {
            await using var trap = new StatisticsTrapRequest(this, context);
            var operation = new RuntimeMicrostateOperation1171();
            try
            {
                trap.OwnPreparation1171(operation.Completion);
                trap.BindPreparationAbort(operation.AbortPreparation);
                NativeResourceDomain524? auxiliary;
                ulong epoch;
                lock (gate)
                {
                    RequireEtsFoundationActor(context);
                    context.Cancellation.ThrowIfCancellationRequested();
                    auxiliary = nativeResources524;
                    epoch = checked(++nextMicrostateCapture1171);
                }
                operation.CaptureTask = CaptureAsyncAccounting1170Async(context, epoch);
                RuntimeAsyncAccountingCapture1170? capture = await operation.CaptureTask;
                if (auxiliary is not null) await auxiliary.Ready989.WaitAsync(context.Cancellation);
                string helper = action == RuntimeMicrostateAction.Gather
                    ? StatisticsTrapNames.GatherMicrostates : StatisticsTrapNames.AwaitMicrostates;
                using StatisticsTrapAdmission source = await Modules.AdmitStatisticsTrap(context, helper,
                    action == RuntimeMicrostateAction.Gather ? 2 : 3);
                RuntimeMicrostateRequest request = Statistics.Microstates.CreateRequest(action, ActorDispatchCapacity);
                request.BindRemoteCount1171(capture?.Count ?? 0);
                request.BindAuxiliary989(auxiliary?.PrepareMicrostate989(request));
                RuntimeSchedulerAuxiliaryBatch420 batch = actorDispatcher.PrepareAuxiliary420(request.Normal);
                operation.Attach(request, batch);
                request.BindStatisticsTrapLifetime1171(
                    batch,
                    trap,
                    operation.Completion,
                    operation.AbortPreparation
                );
                int? ownLane = null;
                bool retry = false;
                lock (gate)
                {
                    RequireEtsFoundationActor(context);
                    context.Cancellation.ThrowIfCancellationRequested();
                    if (!ReferenceEquals(auxiliary, nativeResources524)
                        || !IsAsyncAccountingCaptureCurrent1170(capture)) retry = true;
                    else
                    {
                        if (capture is null)
                            ownLane = actorDispatcher.PublishMicrostateRequest(context, batch, request);
                        else
                        {
                            operation.Tail = actorDispatcher.PrepareMicrostateSourceTail1149(
                                context,
                                batch,
                                request,
                                request.FinishSourcePhase1171
                            );
                            ownLane = actorDispatcher.PublishMicrostatesWithSourceTail1149(context, operation.Tail);
                        }
                        if (ownLane is { } lane) operation.MarkPublished(lane);
                    }
                }
                if (retry) continue;
                if (ownLane is null) return CoreModules.Boolean(request.Previous);
                if (capture is null)
                {
                    request.RunSourcePhase(() => batch.Work[ownLane.Value - ProcessSchedulerNames.First].Execute(ownLane.Value));
                    operation.StartLocal();
                }
                else
                {
                    operation.StartRemote();
                    Exception? sourceFailure = await operation.SourceReady;
                    if (sourceFailure is not null) return await trap.FailAndDrain(sourceFailure);
                }
                Term[] arguments = action == RuntimeMicrostateAction.Gather
                    ? [trap.Reference, Term.I(request.ReplyCount)]
                    : [trap.Reference, CoreModules.Boolean(request.Previous), Term.I(request.ReplyCount)];

                return await trap.RunHelper(
                    source,
                    helper,
                    operation.Completion,
                    arguments
                );
            }
            catch (Exception error)
            {
                // RunHelper already retires the registered trap on both success
                // and failure; never settle that physical owner twice.
                if (trap.Retirement.IsCompleted) throw;
                operation.Fail(error);

                return await trap.FailAndDrain(error);
            }
            finally
            {
                operation.AbandonIfNotStarted();
            }
        }
    }
}
