using System.Runtime.CompilerServices;

namespace Erlang.Compiler;

internal static class PatternArithmetic
{
    // The executable pattern is a literal, while Dbgi preserves the original operator tree.
    private static readonly ConditionalWeakTable<Pattern.Literal, Expr> Sources = new();

    internal static Pattern.Literal? TryFold(Expr expression)
    {
        try
        {
            if (Evaluate(expression) is not { } value)
            {
                return null;
            }

            var pattern = new Pattern.Literal(value);
            Sources.Add(pattern, expression);

            return pattern;
        }
        catch (ErlangException)
        {
            return null;
        }
    }

    internal static Expr Source(Pattern.Literal pattern) =>
        Sources.TryGetValue(pattern, out Expr? expression) ? expression : new Expr.Literal(pattern.Value);

    internal static Term? Evaluate(Expr expression) => expression switch
    {
        Expr.Literal { Value: Integer or FloatTerm } literal => literal.Value,
        Expr.Unary { Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.BitwiseNot } unary
            when Evaluate(unary.Operand) is { } operand => ErlangOperatorValues.Unary(unary.Operator, operand),
        Expr.Binary
        {
            Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply or ErlangOperators.Divide
                or ErlangOperators.IntegerDivide or ErlangOperators.Remainder or ErlangOperators.BitwiseAnd
                or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight
        } binary when Evaluate(binary.Left) is { } left && Evaluate(binary.Right) is { } right =>
            ErlangOperatorValues.Binary(
                binary.Operator,
                left,
                right,
                context: null
            ),
        _ => null
    };
}
