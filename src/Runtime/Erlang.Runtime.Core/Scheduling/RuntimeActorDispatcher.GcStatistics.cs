namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    internal Task GatherGcWithStatisticsTrap(
        ProcessContext requester,
        RuntimeGcParticipantCounters counters,
        StatisticsTrapRequest trap
    )
    {
        if (counters.NormalSchedulers != Capacity)
            throw new InvalidOperationException(GcStatisticsNames.Invalid);
        int own = AssignedLane420(requester);

        return ObserveStatisticsTrapNormalWorkers(
            requester,
            identity =>
            {
                RuntimeGcParticipantSample sample = counters.SampleNormal(identity, identity == own);
                trap.Reply(Term.Tuple(Term.I(identity), new Integer(sample.Collections), new Integer(sample.ReclaimedWords)));
            },
            null,
            trap
        );
    }
}
