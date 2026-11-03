namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeGcParticipantCounters gcParticipantCounters;

    // Capture only an actual physical participant before the collector commits.
    // A Host or another VM's thread cannot contribute a collection to this VM.
    internal int CurrentGcParticipant()
    {
        if (DirtyNativeThread419.Owner is { } dirty)
            return ReferenceEquals(dirty.Runtime, this) && DirtyNativeThread419.Kind != DirtyNativeKind419.Normal
                ? -1 : 0;
        if (!RuntimeActorExecutionTurn.IsRuntime(this))
            return 0;
        int identity = RuntimeActorExecutionTurn.LaneIdentity;
        if (identity < ProcessSchedulerNames.First || identity > gcParticipantCounters.NormalSchedulers)
            throw new InvalidOperationException(GcStatisticsNames.Invalid);

        return identity;
    }

    internal void RecordGcParticipant(int participant, long reclaimedWords)
    {
        if (participant < 0)
            gcParticipantCounters.RecordDirty(reclaimedWords);
        else if (participant > 0)
            gcParticipantCounters.RecordNormal(participant, reclaimedWords);
    }

    internal async ValueTask<Term> SampleGarbageCollection(ProcessContext context)
    {
        using GcStatisticsAdmission helper = await Modules.AdmitGcStatistics(context);
        await using var trap = new StatisticsTrapRequest(this, context, MakeReference());
        Task completion;
        try
        {
            completion = actorDispatcher.GatherGcWithStatisticsTrap(context, gcParticipantCounters, trap);
        }
        catch (Exception failure)
        {
            return await trap.FailAndDrain(failure);
        }

        return await trap.RunGcHelper(helper, completion);
    }
}
