namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeCountersOwner421? counters421;
    private bool countersAdmissionClosed421;
    public RuntimeCountersReport421 CountersStorage421
    {
        get
        {
            lock (gate) return counters421?.Report ?? new(
                0,
                0,
                0,
                0,
                0,
                countersAdmissionClosed421 || disposed
            );
        }
    }

    internal RuntimeCountersOwner421 CountersOwner421
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(countersAdmissionClosed421 || disposed || IsHaltRequested, this);

                return counters421 ??= new();
            }
        }
    }

    internal int RequireCountersActor421(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(CountersNames421.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!RuntimeActorExecutionTurn.IsOwnedBy(context)) throw new InvalidOperationException(CountersNames421.Foreign);
            int lane = RuntimeActorExecutionTurn.LaneIdentity;
            if (lane < ProcessSchedulerNames.First || lane > ActorDispatchCapacity)
                throw new InvalidOperationException(CountersNames421.Foreign);

            return lane;
        }
    }

    private Task CloseCounters421()
    {
        // Latch admission even when no owner exists. Initializer termination
        // can precede the later global disposed transition.
        countersAdmissionClosed421 = true;

        return counters421?.BeginClose() ?? Task.CompletedTask;
    }

    private Task DrainCounters421()
    {
        lock (gate) return CloseCounters421();
    }

    private bool CountersRetirementCompleted421 => counters421 is null || counters421.ShutdownCompleted;

    private Task CountersProcessRetirement421(ProcessContext context, Task previous)
    {
        if (!disposed && !context.Self.Equals(InitializationPid)) return previous;

        return Task.WhenAll(previous, CloseCounters421());
    }
}
