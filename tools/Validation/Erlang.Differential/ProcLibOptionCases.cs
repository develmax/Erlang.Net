namespace Erlang.Differential;

public static class ProcLibOptionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            ProcLibOptionCaseNames.MonitorThree,
            ProcLibOptionCaseSources.MonitorThree,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MonitorFour,
            ProcLibOptionCaseSources.MonitorFour,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MonitorFive,
            ProcLibOptionCaseSources.MonitorFive,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.StartFive,
            ProcLibOptionCaseSources.StartFive,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.LinkFive,
            ProcLibOptionCaseSources.LinkFive,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.StartLinkOption,
            ProcLibOptionCaseSources.StartLinkOption,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.SpawnFun,
            ProcLibOptionCaseSources.SpawnFun,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.SpawnMfa,
            ProcLibOptionCaseSources.SpawnMfa,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.SpawnFunMonitor,
            ProcLibOptionCaseSources.SpawnFunMonitor,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.SpawnLink,
            ProcLibOptionCaseSources.SpawnLink,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.SpawnCustomTag,
            ProcLibOptionCaseSources.SpawnCustomTag,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.SpawnAsyncFalse,
            ProcLibOptionCaseSources.SpawnAsyncFalse,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MonitorErrorAck,
            ProcLibOptionCaseSources.MonitorErrorAck,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.RejectError,
            ProcLibOptionCaseSources.RejectError,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.RejectExit,
            ProcLibOptionCaseSources.RejectExit,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.RejectThrow,
            ProcLibOptionCaseSources.RejectThrow,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.NormalDown,
            ProcLibOptionCaseSources.NormalDown,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ZeroTimeout,
            ProcLibOptionCaseSources.ZeroTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.FiniteTimeout,
            ProcLibOptionCaseSources.FiniteTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.LinkedMonitorTimeout,
            ProcLibOptionCaseSources.LinkedMonitorTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.StartOptionTimeout,
            ProcLibOptionCaseSources.StartOptionTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.LinkOptionTimeout,
            ProcLibOptionCaseSources.LinkOptionTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ImproperArguments,
            ProcLibOptionCaseSources.ImproperArguments,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.NonzeroFun,
            ProcLibOptionCaseSources.NonzeroFun,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.GuardFirst,
            ProcLibOptionCaseSources.GuardFirst,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.InvalidSpawnFun,
            ProcLibOptionCaseSources.InvalidSpawnFun,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.InvalidSpawnMfa,
            ProcLibOptionCaseSources.InvalidSpawnMfa,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ForbiddenAtom,
            ProcLibOptionCaseSources.ForbiddenAtom,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ForbiddenTuple,
            ProcLibOptionCaseSources.ForbiddenTuple,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ForbiddenTagged,
            ProcLibOptionCaseSources.ForbiddenTagged,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MonitorTupleOne,
            ProcLibOptionCaseSources.MonitorTupleOne,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MonitorTupleThree,
            ProcLibOptionCaseSources.MonitorTupleThree,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MemberBeforeKeyfind,
            ProcLibOptionCaseSources.MemberBeforeKeyfind,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.NotListOptions,
            ProcLibOptionCaseSources.NotListOptions,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ImproperOptions,
            ProcLibOptionCaseSources.ImproperOptions,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.ImproperMonitorPrefix,
            ProcLibOptionCaseSources.ImproperMonitorPrefix,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.UnknownOption,
            ProcLibOptionCaseSources.UnknownOption,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.BadAsyncDist,
            ProcLibOptionCaseSources.BadAsyncDist,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.WrapperBadoptStack,
            ProcLibOptionCaseSources.WrapperBadoptStack,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.MonitorInvalidTimeout,
            ProcLibOptionCaseSources.MonitorInvalidTimeout,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        ),
        new(
            ProcLibOptionCaseNames.RegisteredParent,
            ProcLibOptionCaseSources.RegisteredParent,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibOptionCaseNames.Ok))
        )
    ];
}
