namespace Erlang.Compiler;

internal static class BeamComparisonControlFlow
{
    internal static IReadOnlyList<FunctionDefinition> Rewrite(IReadOnlyList<FunctionDefinition> functions) =>
        functions.Select(function => function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(clause.Body, RewriteCase)
            }).ToArray()
        }).ToArray();

    private static Expr RewriteCase(Expr expression)
    {
        if (expression is not Expr.Case
            {
                Value: Expr.Binary comparison,
                Clauses: [var first, var second]
            } decision
            || !IsTotalComparison(comparison)
            || ReadBooleanPattern(first) is not { } startsWithTrue
            || ReadBooleanPattern(second) is not { } endsWithTrue || startsWithTrue == endsWithTrue)
        {
            return expression;
        }

        // Both arms are exhaustive for the comparison result. The operands
        // are already values, so the guard preserves evaluation and failures.
        Clause whenTrue = startsWithTrue ? first : second;
        Clause whenFalse = startsWithTrue ? second : first;

        return new Expr.If(
            [
                whenTrue with { Patterns = [], Guard = comparison },
                whenFalse with { Patterns = [], Guard = new Expr.Literal(Term.A(ErlangBooleanAtoms.True)) }
            ]
        )
        {
            SourceLine = decision.SourceLine,
            PreprocessorFile = decision.PreprocessorFile
        };
    }

    private static bool? ReadBooleanPattern(Clause clause) => clause switch
    {
        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }] } => true,
        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }] } => false,
        _ => null
    };

    private static bool IsTotalComparison(Expr.Binary comparison) =>
        comparison.Left is Expr.Variable or Expr.Literal && comparison.Right is Expr.Variable or Expr.Literal
        && comparison.Operator is ErlangOperators.Less or ErlangOperators.Greater or ErlangOperators.LessOrEqual
            or ErlangOperators.GreaterOrEqual or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
            or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual;
}
