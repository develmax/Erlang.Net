namespace Erlang;

// Temporary admitted operation only. No process/worker back-link is installed
// in permanent accounting records. Completion includes real normal callbacks.
internal sealed class RuntimeMicrostateRequest
{
    private readonly object gate = new();
    private readonly RuntimeMicrostateAccounting owner;
    private readonly int normalCapacity;
    private readonly TaskCompletionSource synchronous = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<RuntimeMicrostateSnapshot> snapshots = new();
    private readonly List<Exception> failures = new();
    private StatisticsTrapRequest? statisticsTrap;
    internal int ReplyCount => checked(normalCapacity + (Action == RuntimeMicrostateAction.Gather ? Unmanaged.Length : 0));
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
        snapshots.EnsureCapacity(checked(normalCapacity + count));
        failures.EnsureCapacity(checked(count + 2));
    }

    internal void Bind(Task normalCompletion) => Completion = Task.WhenAll(normalCompletion, synchronous.Task);

    internal void BindStatisticsTrap(RuntimeSchedulerAuxiliaryBatch420 batch, StatisticsTrapRequest trap)
    {
        statisticsTrap = trap;
        Bind(batch.Completion);
        trap.Observe(batch);
        trap.BindPhysical(Completion);
    }

    internal void AbortStatisticsTrap(Exception error)
    {
        Failure(error);
        synchronous.TrySetException(error);
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
        foreach (RuntimeMicrostateRecord record in Unmanaged)
        {
            try
            {
                RuntimeMicrostateSnapshot? snapshot = owner.ApplyUnmanagedSnapshot(this, record);
                if (Action == RuntimeMicrostateAction.Gather)
                    statisticsTrap?.Reply(snapshot!.ToTerm());
            }
            catch (Exception error)
            {
                Failure(error);
            }
        }
        try
        {
            ownNormal();
        }
        catch (Exception error)
        {
            Failure(error);
        }
        try
        {
            owner.FinishRequest(this);
        }
        catch (Exception error)
        {
            Failure(error);
        }
        finally
        {
            lock (gate)
            {
                if (failures.Count == 0) synchronous.TrySetResult();
                else if (statisticsTrap is null) synchronous.TrySetException(new AggregateException(failures));
                else synchronous.TrySetException(failures);
            }
        }
    }

    internal Term Result()
    {
        if (Action != RuntimeMicrostateAction.Gather)
            return CoreModules.Boolean(Previous);
        lock (gate) return Cons.From(snapshots.Select(value => value.ToTerm()));
    }
}
