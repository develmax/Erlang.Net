namespace Erlang.Differential;

public static class ProcessErrorHandlerCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules
    {
        get;
    } =
    [
        Case(ProcessErrorHandlerCaseNames.Literal, ProcessErrorHandlerCaseNames.LiteralBody),
        Case(ProcessErrorHandlerCaseNames.NonTail, ProcessErrorHandlerCaseNames.NonTailBody),
        Case(ProcessErrorHandlerCaseNames.Apply, ProcessErrorHandlerCaseNames.ApplyBody),
        Case(ProcessErrorHandlerCaseNames.ExternalFun, ProcessErrorHandlerCaseNames.ExternalFunBody),
        Case(ProcessErrorHandlerCaseNames.Nested, ProcessErrorHandlerCaseNames.NestedBody),
        Case(ProcessErrorHandlerCaseNames.Previous, ProcessErrorHandlerCaseNames.PreviousBody),
        Case(ProcessErrorHandlerCaseNames.Invalid, ProcessErrorHandlerCaseNames.InvalidBody),
        Case(ProcessErrorHandlerCaseNames.Absent, ProcessErrorHandlerCaseNames.AbsentBody),
        Case(ProcessErrorHandlerCaseNames.NoInterception, ProcessErrorHandlerCaseNames.NoInterceptionBody),
        Case(ProcessErrorHandlerCaseNames.Child, ProcessErrorHandlerCaseNames.ChildBody),
        Case(ProcessErrorHandlerCaseNames.ErrorFrames, ProcessErrorHandlerCaseNames.ErrorFramesBody),
        Case(ProcessErrorHandlerCaseNames.ThrowFrames, ProcessErrorHandlerCaseNames.ThrowFramesBody),
        Case(ProcessErrorHandlerCaseNames.ExitFrames, ProcessErrorHandlerCaseNames.ExitFramesBody),
        Case(ProcessErrorHandlerCaseNames.ErrorTwo, ProcessErrorHandlerCaseNames.ErrorTwoBody),
        Case(ProcessErrorHandlerCaseNames.ErrorThree, ProcessErrorHandlerCaseNames.ErrorThreeBody),
        Case(ProcessErrorHandlerCaseNames.NestedError, ProcessErrorHandlerCaseNames.NestedErrorBody),
        Case(ProcessErrorHandlerCaseNames.NestedErrorOne, ProcessErrorHandlerCaseNames.NestedErrorOneBody),
        Case(ProcessErrorHandlerCaseNames.ExplicitEmpty, ProcessErrorHandlerCaseNames.ExplicitEmptyBody),
        Case(ProcessErrorHandlerCaseNames.ExplicitCustom, ProcessErrorHandlerCaseNames.ExplicitCustomBody),
        Case(ProcessErrorHandlerCaseNames.Evaluation, ProcessErrorHandlerCaseNames.EvaluationBody)
    ];

    private static CompiledModuleCase Case(string name, string body) => new(
        ProcessErrorHandlerCaseNames.Prefix + name,
        ProcessErrorHandlerCaseNames.ModulePrefix + body + ProcessErrorHandlerCaseNames.ModuleSuffix,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
