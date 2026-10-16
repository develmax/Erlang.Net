namespace Erlang.Differential;

public static class ProcLibStopCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            ProcLibStopCaseNames.DefaultNormal,
            ProcLibStopCaseSources.DefaultNormal,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.ExactReason,
            ProcLibStopCaseSources.ExactReason,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.EqualExitBeforeAck,
            ProcLibStopCaseSources.EqualExitBeforeAck,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.DifferentExitBeforeAck,
            ProcLibStopCaseSources.DifferentExitBeforeAck,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.AckThenDifferentExit,
            ProcLibStopCaseSources.AckThenDifferentExit,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.MissingProcess,
            ProcLibStopCaseSources.MissingProcess,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.MissingName,
            ProcLibStopCaseSources.MissingName,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.StopSelf,
            ProcLibStopCaseSources.StopSelf,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.NamedStop,
            ProcLibStopCaseSources.NamedStop,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.NamedTupleStop,
            ProcLibStopCaseSources.NamedTupleStop,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.SysReturnsBeforeDeath,
            ProcLibStopCaseSources.SysReturnsBeforeDeath,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.StopWaitsForDeath,
            ProcLibStopCaseSources.StopWaitsForDeath,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.TimeoutPreservesTarget,
            ProcLibStopCaseSources.TimeoutPreservesTarget,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.AliasDropsLateReply,
            ProcLibStopCaseSources.AliasDropsLateReply,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.SysMissingName,
            ProcLibStopCaseSources.SysMissingName,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.SysSelf,
            ProcLibStopCaseSources.SysSelf,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.ViaRegistry,
            ProcLibStopCaseSources.ViaRegistry,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.ViaMissing,
            ProcLibStopCaseSources.ViaMissing,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.SysDisconnected,
            ProcLibStopCaseSources.SysDisconnected,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.MailboxOrder,
            ProcLibStopCaseSources.MailboxOrder,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.SysInvalidTimeout,
            ProcLibStopCaseSources.SysInvalidTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.StopInvalidTimeout,
            ProcLibStopCaseSources.StopInvalidTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.GenServerAckBeforeCallback,
            ProcLibStopCaseSources.GenServerAckBeforeCallback,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.GenServerStopWaitsForCallback,
            ProcLibStopCaseSources.GenServerStopWaitsForCallback,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.SupervisorAckBeforeChildShutdown,
            ProcLibStopCaseSources.SupervisorAckBeforeChildShutdown,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.AfterAckTimeoutWaitsForRealDown,
            ProcLibStopCaseSources.AfterAckTimeoutWaitsForRealDown,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.OneTimeoutBudget,
            ProcLibStopCaseSources.OneTimeoutBudget,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.LocalInfinityNoConnection,
            ProcLibStopCaseSources.LocalInfinityNoConnection,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        ),
        new(
            ProcLibStopCaseNames.LocalFiniteNoConnection,
            ProcLibStopCaseSources.LocalFiniteNoConnection,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(OracleOutcomeTags.Success))
        )
    ];
}
