namespace Erlang.Compiler;

internal sealed class BeamConditionalExpression(Func<string, bool> defined, int offset)
{
    internal bool IsTrue(Expr expression)
    {
        Validate(expression);

        return Evaluate(expression) is Atom { Name: "true" };
    }

    private void Validate(Expr expression)
    {
        switch (expression)
        {
            case Expr.Literal:
            case Expr.Call { Module: null, Function: "defined", Arguments: [Expr.Variable or Expr.Literal { Value: Atom }] }:
                return;
            case Expr.Unary { Operator: ErlangOperators.Not, Operand: var operand }:
                Validate(operand);
                return;
            case Expr.Binary binary when IsBinaryOperator(binary.Operator):
                Validate(binary.Left);
                Validate(binary.Right);
                return;
            default:
                throw Unsupported();
        }
    }

    private static bool IsBinaryOperator(string op) => op is
        ErlangOperators.AndAlso or ErlangOperators.OrElse or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual
        or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual or ErlangOperators.Less or ErlangOperators.LessOrEqual
        or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual;

    private Term Evaluate(Expr expression) => expression switch
    {
        Expr.Literal literal => literal.Value,
        Expr.Call { Arguments: [Expr.Variable variable] } => Boolean(defined(variable.Name)),
        Expr.Call { Arguments: [Expr.Literal { Value: Atom atom }] } => Boolean(defined(atom.Name)),
        Expr.Unary unary => Boolean(!RequireBoolean(Evaluate(unary.Operand))),
        Expr.Binary binary => EvaluateBinary(binary),
        _ => throw Unsupported()
    };

    private Term EvaluateBinary(Expr.Binary binary)
    {
        Term left = Evaluate(binary.Left);
        if (binary.Operator == ErlangOperators.AndAlso)
        {
            return RequireBoolean(left) ? Evaluate(binary.Right) : left;
        }
        if (binary.Operator == ErlangOperators.OrElse)
        {
            return RequireBoolean(left) ? left : Evaluate(binary.Right);
        }
        Term right = Evaluate(binary.Right);

        return Boolean(binary.Operator switch
        {
            ErlangOperators.ExactEqual => left.Equals(right),
            ErlangOperators.ExactNotEqual => !left.Equals(right),
            ErlangOperators.NumericEqual => TermOrder.Compare(left, right) == 0,
            ErlangOperators.NumericNotEqual => TermOrder.Compare(left, right) != 0,
            ErlangOperators.Less => TermOrder.Compare(left, right) < 0,
            ErlangOperators.LessOrEqual => TermOrder.Compare(left, right) <= 0,
            ErlangOperators.Greater => TermOrder.Compare(left, right) > 0,
            ErlangOperators.GreaterOrEqual => TermOrder.Compare(left, right) >= 0,
            _ => throw Unsupported()
        });
    }

    private bool RequireBoolean(Term term) => term switch
    {
        Atom { Name: "true" } => true,
        Atom { Name: "false" } => false,
        _ => throw Unsupported()
    };

    private static Atom Boolean(bool value) => Term.A(value ? "true" : "false");

    private CompileException Unsupported() =>
        new(CompilerDiagnosticCodes.UnsupportedSyntax, BeamMacroDiagnostics.UnsupportedCondition, offset);
}
