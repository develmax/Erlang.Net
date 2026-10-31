namespace Erlang;

public sealed partial class ProcessRuntime
{
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
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
        string helper = action == RuntimeMicrostateAction.Gather
            ? StatisticsTrapNames.GatherMicrostates : StatisticsTrapNames.AwaitMicrostates;
        RuntimeMicrostateRequest request = Statistics.Microstates.CreateRequest(action, ActorDispatchCapacity);
        var trap = new StatisticsTrapRequest(this, context);
        RuntimeSchedulerAuxiliaryBatch420 batch = actorDispatcher.PrepareAuxiliary420(request.Normal);
        request.BindStatisticsTrap(batch, trap);
        int? ownLane;
        try
        {
            lock (gate)
            {
                RequireEtsFoundationActor(context);
                context.Cancellation.ThrowIfCancellationRequested();
                ownLane = actorDispatcher.PublishMicrostateRequest(context, batch, request);
            }
        }
        catch (Exception error)
        {
            var failures = new List<Exception> { error };
            try
            {
                batch.Abort(error);
            }
            catch (Exception abort)
            {
                failures.Add(abort);
            }
            request.AbortStatisticsTrap(error);

            return await trap.FailAndDrain(failures.Count == 1 ? error : new AggregateException(failures));
        }
        if (ownLane is null)
            return CoreModules.Boolean(request.Previous);
        request.RunSourcePhase(() => batch.Work[ownLane.Value - ProcessSchedulerNames.First].Execute(ownLane.Value));
        StatisticsTrapAdmission? source = null;
        Term[] arguments;
        try
        {
            source = await Modules.AdmitStatisticsTrap(context, helper,
                action == RuntimeMicrostateAction.Gather ? 2 : 3);
            arguments = action == RuntimeMicrostateAction.Gather
                ? [trap.Reference, Term.I(request.ReplyCount)]
                : [trap.Reference, CoreModules.Boolean(request.Previous), Term.I(request.ReplyCount)];
        }
        catch (Exception error)
        {
            source?.Dispose();

            return await trap.FailAndDrain(error);
        }
        using (source)
            return await trap.RunHelper(
                source!,
                helper,
                request.Completion,
                arguments
            );
    }
}
