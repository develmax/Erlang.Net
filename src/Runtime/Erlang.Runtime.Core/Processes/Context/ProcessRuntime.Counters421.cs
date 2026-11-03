namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeCountersOwner421? counters421;
    private bool countersAdmissionClosed421;
    private Task? countersShutdown596;
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
        lock (gate)
        {
            countersAdmissionClosed421 = true;
            if (countersShutdown596 is not null)
                return countersShutdown596;
            RuntimeCountersOwner421? selected = counters421;
            if (selected is null)
                return countersShutdown596 = Task.CompletedTask;
            selected.StopAdmission596();
            if (ExecutionContext.IsFlowSuppressed())
                return countersShutdown596 = Task.Run(() => CloseCountersAfterRoots596(selected));
            using (ExecutionContext.SuppressFlow())
                return countersShutdown596 = Task.Run(() => CloseCountersAfterRoots596(selected));
        }
    }

    private async Task CloseCountersAfterRoots596(RuntimeCountersOwner421 selected)
    {
        // Join logical transition publication, then drain real roots without Runtime.gate.
        lock (gate)
        {
        }
        await DrainOwnedTermContexts506().ConfigureAwait(false);
        await selected.BeginClose().ConfigureAwait(false);
    }

    private Task DrainCounters421() => CloseCounters421();

    private bool CountersRetirementCompleted421 => countersShutdown596?.IsCompletedSuccessfully
        ?? (counters421 is null || counters421.ShutdownCompleted);

    private Task CountersProcessRetirement421(ProcessContext context, Task previous)
    {
        if (context.Self.Equals(InitializationPid))
            _ = CloseCounters421();

        // Per-process completion must not join the VM-wide root/owner drain.
        // InitializationEnded and final Dispose/Halt retain their explicit
        // CloseCounters421/DrainCounters421 calls. Individual roots retire via
        // the process heap and the backing owner's last-root queue.
        return previous;
    }
}
