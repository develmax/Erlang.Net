namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasBooleanCaseNestedListSiblings(
        FunctionDefinition function,
        IReadOnlyList<Expr.ListComprehension> comprehensions
    )
    {
        if (function.Clauses is not [{ Guard: null } clause] || !HasUnconditionalParameters(clause)
            || comprehensions is not [var whenTrue, var whenFalse])
        {
            return false;
        }

        // The boolean decision owns one complete generator chain on each arm.
        // Existing Core and SSA walks order these arms independently of the
        // widths of their letrecs, and either chain must lower in full.
        return IsPair(clause.Body)
            || clause.Body is Expr.Sequence sequence && sequence.Items.Any(item =>
                item is Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard }, Value: var value }
                && IsPair(value));

        bool IsPair(Expr expression) => expression is Expr.Case
        {
            Value: Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: Expr.Variable, Right: var constant },
            Clauses:
            [
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }], Body: var yes },
            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }], Body: var no }
            ]
        } && ReferenceEquals(yes, whenTrue) && ReferenceEquals(no, whenFalse)
            && AttributeLiteral.TryNormalizeConstant(constant, out _)
            && HasTwoOrdinaryGenerators(whenTrue) && HasTwoOrdinaryGenerators(whenFalse);

        static bool HasTwoOrdinaryGenerators(Expr.ListComprehension comprehension) =>
            comprehension.Qualifiers is
            [
                ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable },
                ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable }
            ];
    }
}
