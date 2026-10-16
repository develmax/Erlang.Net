namespace Erlang.Differential;

public static class ReceiveWaitCases
{
    private static IReadOnlyList<(string Name, string Source, string Expression, Term Expected)> Definitions { get; } = Create();
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Definitions.Select(value => new CompiledModuleCase(value.Name, value.Source, Term.Tuple(Term.A(OracleOutcomeTags.Success), value.Expected))).ToArray();
    public static IReadOnlyList<(string Source, Term Expected)> Interpreted { get; } = Definitions.Select(value => (value.Expression, value.Expected)).ToArray();

    private static IReadOnlyList<(string Name, string Source, string Expression, Term Expected)> Create()
    {
        Term matched = Term.A(ReceiveWaitCaseNames.Matched);
        Term caughtAndPreserved = Term.Tuple(Term.A(ReceiveWaitCaseNames.Caught), Term.A(ReceiveWaitCaseNames.Preserved));
        Term evaluated = Term.A(ReceiveWaitCaseNames.Evaluated);

        return [
            (ReceiveWaitCaseNames.QueuedAtom, ReceiveWaitCaseSources.QueuedAtom, ReceiveWaitCaseSources.InterpretedQueuedAtom, matched),
            (ReceiveWaitCaseNames.QueuedNegative, ReceiveWaitCaseSources.QueuedNegative, ReceiveWaitCaseSources.InterpretedQueuedNegative, matched),
            (ReceiveWaitCaseNames.QueuedFloat, ReceiveWaitCaseSources.QueuedFloat, ReceiveWaitCaseSources.InterpretedQueuedFloat, matched),
            (ReceiveWaitCaseNames.QueuedAboveLimit, ReceiveWaitCaseSources.QueuedAboveLimit, ReceiveWaitCaseSources.InterpretedQueuedAboveLimit, matched),
            (ReceiveWaitCaseNames.UnmatchedInvalid, ReceiveWaitCaseSources.UnmatchedInvalid, ReceiveWaitCaseSources.InterpretedUnmatchedInvalid, caughtAndPreserved),
            (ReceiveWaitCaseNames.ExpressionBeforeMatch, ReceiveWaitCaseSources.ExpressionBeforeMatch, ReceiveWaitCaseSources.InterpretedExpressionBeforeMatch, Term.Tuple(Term.Tuple(matched, evaluated), evaluated)),
            (ReceiveWaitCaseNames.ExpressionError, ReceiveWaitCaseSources.ExpressionError, ReceiveWaitCaseSources.InterpretedExpressionError, caughtAndPreserved),
            (ReceiveWaitCaseNames.QueuedZero, ReceiveWaitCaseSources.QueuedZero, ReceiveWaitCaseSources.InterpretedQueuedZero, matched)
        ];
    }
}
