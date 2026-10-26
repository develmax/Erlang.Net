namespace Erlang.Differential;

public static class SpawnAsyncDistributionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules
    {
        get;
    } =
    [
        Case(SpawnAsyncDistributionCaseNames.Fun, SpawnAsyncDistributionCaseNames.FunBody),
        Case(SpawnAsyncDistributionCaseNames.LocalFun, SpawnAsyncDistributionCaseNames.LocalFunBody),
        Case(SpawnAsyncDistributionCaseNames.Mfa, SpawnAsyncDistributionCaseNames.MfaBody),
        Case(SpawnAsyncDistributionCaseNames.LocalMfa, SpawnAsyncDistributionCaseNames.LocalMfaBody),
        Case(SpawnAsyncDistributionCaseNames.False, SpawnAsyncDistributionCaseNames.FalseBody),
        Case(SpawnAsyncDistributionCaseNames.Noninheritance, SpawnAsyncDistributionCaseNames.NoninheritanceBody),
        Case(SpawnAsyncDistributionCaseNames.LinkMonitor, SpawnAsyncDistributionCaseNames.LinkMonitorBody),
        Case(SpawnAsyncDistributionCaseNames.RequestFun, SpawnAsyncDistributionCaseNames.RequestFunBody),
        Case(SpawnAsyncDistributionCaseNames.RequestMfa, SpawnAsyncDistributionCaseNames.RequestMfaBody),
        Case(SpawnAsyncDistributionCaseNames.RequestFalse, SpawnAsyncDistributionCaseNames.RequestFalseBody),
        Case(SpawnAsyncDistributionCaseNames.Invalid, SpawnAsyncDistributionCaseNames.InvalidBody),
        Case(SpawnAsyncDistributionCaseNames.RequestInvalid, SpawnAsyncDistributionCaseNames.RequestInvalidBody),
        Case(SpawnAsyncDistributionCaseNames.Disconnected, SpawnAsyncDistributionCaseNames.DisconnectedBody),
        Case(SpawnAsyncDistributionCaseNames.Evaluation, SpawnAsyncDistributionCaseNames.EvaluationBody)
    ];

    private static CompiledModuleCase Case(string name, string body) => new(
        SpawnAsyncDistributionCaseNames.Prefix + name,
        SpawnAsyncDistributionCaseNames.ModulePrefix + body + SpawnAsyncDistributionCaseNames.ModuleSuffix,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
