namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Expr.Binary Append, Expr.Literal Tail)? ReadLiteralListComprehensionAppend(
        Clause clause,
        Expr.ListComprehension comprehension
    )
    {
        if (comprehension is not
            {
                Items: [Expr.Binary
                {
                    Operator: ErlangOperators.Append, Left: Expr.Variable head, Right: var suffix
                }],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
                }]
            }
            || item.Name == VariableScopeNames.Wildcard || item.Name != head.Name
            || !TryReadLiteralNonemptyList(suffix, out _)
            || !HasDominatingListBinding(
                clause,
                comprehension,
                source.Name,
                IsProperListValue,
                walkAppendConsumer: true
            ))
        {
            return null;
        }

        var expressions = LocalFunctionEntries.Walk(clause.Body).ToArray();
        if (expressions.Any(expression => expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe))
        {
            return null;
        }

        foreach (Expr.Binary append in expressions.OfType<Expr.Binary>())
        {
            if (append.Operator == ErlangOperators.Append && ReferenceEquals(append.Left, comprehension)
                && TryReadLiteralNonemptyList(append.Right, out Term tail))
            {
                // The right operand is already a value and can be the helper's nil result.
                return (append, new Expr.Literal(tail));
            }
        }

        return null;
    }

    private static bool TryReadLiteralNonemptyList(Expr expression, out Term value)
    {
        if (expression is (Expr.List or Expr.Literal)
            && AttributeLiteral.TryNormalizeConstant(expression, out value) && value is Cons)
        {
            Term rest = value;
            while (rest is Cons cell)
            {
                rest = cell.Tail;
            }

            return rest is Nil;
        }

        value = Nil.Value;

        return false;
    }
}
