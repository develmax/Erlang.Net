namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Task DispatchInitializer(ProcessContext context, Func<Task> body)
        => actorDispatcher.Dispatch(context, async () =>
        {
            lock (gate)
                context.ExecutionStarted = true;
            await body();
        }, RuntimeActorRole.Initialization);

    internal async ValueTask<Term> ChangeSchedulerBinding(ProcessContext context, Term value)
    {
        int binding = ProcessSchedulerValues.Parse(value, ActorDispatchCapacity);
        if (context.ActorTaskScheduler is null || !RuntimeActorExecutionTurn.IsOwnedBy(context))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        int previous;
        lock (gate)
        {
            RequireAlive(context.Self);
            previous = actorDispatcher.ChangeBinding(context, binding);
        }
        if (binding != ProcessSchedulerNames.Unbound && RuntimeActorExecutionTurn.LaneIdentity != binding)
            await context.YieldExecution();
        context.Cancellation.ThrowIfCancellationRequested();

        return Term.I(previous);
    }

    internal async ValueTask<Term> ChangeSchedulersOnline(ProcessContext context, Term value)
    {
        int count = ProcessSchedulerValues.Parse(value, ActorDispatchCapacity);
        if (count == ProcessSchedulerNames.Unbound)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
            RequireAlive(context.Self);
        int previous = await actorDispatcher.ChangeOnline(context, count);
        if (RuntimeActorExecutionTurn.LaneIdentity > actorDispatcher.OnlineCount)
            await context.YieldExecution();
        context.Cancellation.ThrowIfCancellationRequested();

        return Term.I(previous);
    }

    internal bool TrySchedulerInfo(ProcessContext context, Term query, out Term result)
    {
        result = Nil.Value;
        if (TryDirtySchedulerInfo419(context, query, out result)) return true;
        if (query is not Atom atom)
            return false;
        var state = actorDispatcher.State();
        switch (atom.Name)
        {
            case ProcessSchedulerNames.Identity:
                if (!RuntimeActorExecutionTurn.IsOwnedBy(context))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                result = Term.I(RuntimeActorExecutionTurn.LaneIdentity);

                return true;
            case ProcessSchedulerNames.Total:
                result = Term.I(ActorDispatchCapacity);

                return true;
            case ProcessSchedulerNames.Online:
                result = Term.I(state.Online);

                return true;
            default:
                return false;
        }
    }
}
