namespace Erlang;

public sealed partial class ProcessRuntime
{
    public RuntimeManagedSchedulerWallTimeSnapshot CaptureManagedSchedulerWallTime()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();

            return actorDispatcher.WallTimeSnapshot();
        }
    }

    internal bool ExchangeSchedulerWallTime(ProcessContext context, bool enabled)
    {
        if (!ReferenceEquals(context.Runtime, this))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        context.Cancellation.ThrowIfCancellationRequested();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();
            RequireAlive(context.Self);
            if (context.ActorTaskScheduler is null || !RuntimeActorExecutionTurn.IsOwnedBy(context))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return actorDispatcher.ExchangeWallTime(enabled);
        }
    }
}
