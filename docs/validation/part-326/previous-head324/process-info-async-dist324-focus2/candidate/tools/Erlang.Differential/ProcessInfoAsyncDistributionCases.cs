namespace Erlang.Differential;

public static class ProcessInfoAsyncDistributionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules
    {
        get;
    } =
    [
        Case(ProcessInfoAsyncDistributionCaseNames.Default, ProcessInfoAsyncDistributionCaseNames.DefaultBody),
        Case(ProcessInfoAsyncDistributionCaseNames.Transitions, ProcessInfoAsyncDistributionCaseNames.TransitionsBody),
        Case(ProcessInfoAsyncDistributionCaseNames.List, ProcessInfoAsyncDistributionCaseNames.ListBody),
        Case(ProcessInfoAsyncDistributionCaseNames.Child, ProcessInfoAsyncDistributionCaseNames.ChildBody),
        Case(ProcessInfoAsyncDistributionCaseNames.Dead, ProcessInfoAsyncDistributionCaseNames.DeadBody),
        Case(ProcessInfoAsyncDistributionCaseNames.Unknown, ProcessInfoAsyncDistributionCaseNames.UnknownBody)
    ];

    private static CompiledModuleCase Case(string name, string body) => new(
        ProcessInfoAsyncDistributionCaseNames.Prefix + name,
        ProcessInfoAsyncDistributionCaseNames.ModulePrefix + body + ProcessInfoAsyncDistributionCaseNames.ModuleSuffix,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
