namespace Erlang;

// Temporary admitted operation only. No process/worker back-link is installed
// in permanent accounting records. Completion includes real normal callbacks.
internal sealed partial class RuntimeMicrostateRequest
{
    private readonly object gate = new();
    private readonly RuntimeMicrostateAccounting owner;
    private readonly int normalCapacity;
    private readonly TaskCompletionSource synchronous = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<RuntimeMicrostateSnapshot> snapshots = new();
    private readonly List<Exception> failures = new();
    private StatisticsTrapRequest? statisticsTrap;
    private NativeResourceMicrostateWork989? auxiliary989;
    internal int ReplyCount => checked(normalCapacity + (auxiliary989 is null ? 0 : 1) + (Action == RuntimeMicrostateAction.Gather ? checked(Unmanaged.Length + remoteCount1171) : 0));
    internal RuntimeMicrostateAction Action { get; }
    internal bool Previous { get; set; }
    internal bool NoOp { get; set; }
    internal RuntimeMicrostateRecord[] Unmanaged { get; set; } = [];
    internal Task Completion { get; private set; } = Task.CompletedTask;

    internal RuntimeMicrostateRequest(RuntimeMicrostateAccounting owner, RuntimeMicrostateAction action, int normalCapacity)
    {
        this.owner = owner;
        this.normalCapacity = normalCapacity;
        Action = action;
    }

    internal void ReserveUnmanaged(int count)
    {
        snapshots.EnsureCapacity(checked(normalCapacity + count + (auxiliary989 is null ? 0 : 1) + (Action == RuntimeMicrostateAction.Gather ? remoteCount1171 : 0)));
        failures.EnsureCapacity(checked(count + remoteCount1171 + RuntimeMicrostateOperationNames1171.SourceFailureReserve));
    }

    internal void BindAuxiliary989(NativeResourceMicrostateWork989? auxiliary) => auxiliary989 = auxiliary;

    internal void PublishAuxiliary989() => auxiliary989?.Publish();

    internal void Bind(Task normalCompletion)
        => Completion = Task.WhenAll(normalCompletion, synchronous.Task, auxiliary989?.Completion ?? Task.CompletedTask);

    internal void BindStatisticsTrap(RuntimeSchedulerAuxiliaryBatch420 batch, StatisticsTrapRequest trap)
    {
        statisticsTrap = trap;
        trap.BindPreparationAbort(AbortStatisticsTrap);
        Bind(batch.Completion);
        trap.Observe(batch);
        trap.BindPhysical(Completion);
    }

    internal void AbortStatisticsTrap(Exception error)
    {
        Exception? reporting = null;
        Exception? auxiliary = null;
        try
        {
            Failure(error);
        }
        catch (Exception failure)
        {
            reporting = failure;
        }
        try
        {
            auxiliary989?.AbortPrepared(error);
        }
        catch (Exception failure)
        {
            auxiliary = failure;
        }
        finally
        {
            synchronous.TrySetException(error);
        }

        if (reporting is null && auxiliary is null) return;
        var errors = new StatisticsTrapFailures();
        if (reporting is not null) errors.Add(reporting);
        if (auxiliary is not null) errors.Add(auxiliary);
        errors.ThrowIfAny();
    }

    internal bool Prepare()
    {
        if (!owner.PrepareRequest(this))
            return false;
        // Pinned no-op returns before allocating any actual request Ref.
        statisticsTrap?.PrepareReference();

        return true;
    }

    internal void Normal(int unusedLaneIdentity)
    {
        RuntimeMicrostateSnapshot? snapshot = owner.ApplyNormalSnapshot(this);
        statisticsTrap?.Reply(snapshot?.ToTerm());
    }

    internal void Auxiliary989()
    {
        RuntimeMicrostateSnapshot? snapshot = owner.ApplyAuxiliarySnapshot989(this);
        statisticsTrap?.Reply(snapshot?.ToTerm());
    }

    internal void Add(RuntimeMicrostateSnapshot value)
    {
        lock (gate) snapshots.Add(value);
    }

    internal void Failure(Exception value)
    {
        lock (gate) failures.Add(value);
        statisticsTrap?.ReceiveFailure.Record(value);
    }

    internal void RunSourcePhase(Action ownNormal)
    {
        ApplyLocalUnmanaged1171();
        try
        {
            ownNormal();
        }
        catch (Exception error)
        {
            Failure(error);
        }
        FinishSourcePhase1171();
    }

    internal Term Result()
    {
        if (Action != RuntimeMicrostateAction.Gather)
            return CoreModules.Boolean(Previous);
        lock (gate) return Cons.From(snapshots.Select(value => value.ToTerm()));
    }
}
