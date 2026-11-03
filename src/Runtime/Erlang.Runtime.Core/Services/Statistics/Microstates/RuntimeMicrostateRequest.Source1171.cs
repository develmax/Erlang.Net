namespace Erlang;

internal sealed partial class RuntimeMicrostateRequest
{
    private int localSource1171;
    private int finishedSource1171;
    private int remoteCount1171;

    internal void BindRemoteCount1171(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (Volatile.Read(ref localSource1171) != 0)
            throw new InvalidOperationException(RuntimeMicrostateOperationNames1171.Phase);

        remoteCount1171 = count;
    }

    internal void ApplyLocalUnmanaged1171()
    {
        if (Interlocked.CompareExchange(ref localSource1171, 1, 0) != 0)
            throw new InvalidOperationException(RuntimeMicrostateOperationNames1171.Phase);

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
    }

    internal void ApplyRemoteSnapshots1171(RuntimeMicrostateSnapshot[] rows)
    {
        int expected = Action == RuntimeMicrostateAction.Gather ? remoteCount1171 : 0;
        if (rows.Length != expected)
            throw new InvalidDataException(RuntimeMicrostateOperationNames1171.Roster);

        for (int index = rows.Length; index != 0;)
        {
            RuntimeMicrostateSnapshot snapshot = rows[--index];
            try
            {
                Add(snapshot);
                statisticsTrap?.Reply(snapshot.ToTerm());
            }
            catch (Exception error)
            {
                Failure(error);
            }
        }
    }

    internal void FinishSourcePhase1171()
    {
        if (Volatile.Read(ref localSource1171) == 0
            || Interlocked.CompareExchange(ref finishedSource1171, 1, 0) != 0)
            throw new InvalidOperationException(RuntimeMicrostateOperationNames1171.Phase);

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

    internal void BindStatisticsTrapLifetime1171(
        RuntimeSchedulerAuxiliaryBatch420 batch,
        StatisticsTrapRequest trap,
        Task physical,
        Action<Exception> abort
    )
    {
        statisticsTrap = trap;
        trap.BindPreparationAbort(abort);
        Bind(batch.Completion);
        trap.ObserveWithPhysical1171(batch, physical);
        trap.BindPhysical(physical);
    }
}
