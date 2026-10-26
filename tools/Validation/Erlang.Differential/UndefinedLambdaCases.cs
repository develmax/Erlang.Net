namespace Erlang.Differential;

public static class UndefinedLambdaCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules
    {
        get;
    } =
    [
        Case(UndefinedLambdaCaseNames.NoCurrent, UndefinedLambdaCaseNames.NoCurrentBody),
        Case(UndefinedLambdaCaseNames.MissingHandler, UndefinedLambdaCaseNames.MissingHandlerBody),
        Case(UndefinedLambdaCaseNames.BadArity, UndefinedLambdaCaseNames.BadArityBody),
        Case(UndefinedLambdaCaseNames.Propagation, UndefinedLambdaCaseNames.PropagationBody),
        Case(UndefinedLambdaCaseNames.CurrentIncompatible, UndefinedLambdaCaseNames.CurrentIncompatibleBody),
        Case(UndefinedLambdaCaseNames.Isolation, UndefinedLambdaCaseNames.IsolationBody),
        Case(UndefinedLambdaCaseNames.ActiveOld, UndefinedLambdaCaseNames.ActiveOldBody),
        Case(UndefinedLambdaCaseNames.Evaluation, UndefinedLambdaCaseNames.EvaluationBody)
    ];

    private static CompiledModuleCase Case(string name, string body) => new(
        UndefinedLambdaCaseNames.Prefix + name,
        UndefinedLambdaCaseNames.ModulePrefix + body + UndefinedLambdaCaseNames.ModuleSuffix,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
