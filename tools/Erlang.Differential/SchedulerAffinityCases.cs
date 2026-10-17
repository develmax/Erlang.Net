namespace Erlang.Differential;

public static class SchedulerAffinityCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            SchedulerAffinityCaseNames.FirstInstruction,
            SchedulerAffinityCaseSources.FirstInstruction,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SchedulerAffinityCaseNames.Done))
        ),
        new(
            SchedulerAffinityCaseNames.MigrationPreviousBinding,
            SchedulerAffinityCaseSources.MigrationPreviousBinding,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SchedulerAffinityCaseNames.Done))
        ),
        new(
            SchedulerAffinityCaseNames.InvalidAndDuplicate,
            SchedulerAffinityCaseSources.InvalidAndDuplicate,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SchedulerAffinityCaseNames.Done))
        ),
        new(
            SchedulerAffinityCaseNames.OfflineBoundResume,
            SchedulerAffinityCaseSources.OfflineBoundResume,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SchedulerAffinityCaseNames.Done))
        ),
        new(
            SchedulerAffinityCaseNames.OfflinePendingKill,
            SchedulerAffinityCaseSources.OfflinePendingKill,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SchedulerAffinityCaseNames.Done))
        )
    ];
}
