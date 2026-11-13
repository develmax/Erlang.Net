namespace Erlang.Compiler;

internal static class AttributeLiteral
{
    internal static bool TryNormalizeConstant(Expr expression, out Term value, IReadOnlyDictionary<string, Term>? bindings = null)
    {
        try
        {
            value = Read(expression, 0, false, allowExpressionConstants: true, bindings);

            return true;
        }
        catch (Exception error) when (error is CompileException or ErlangException)
        {
            value = Nil.Value;

            return false;
        }
    }

    public static Term Normalize(Expr expression, int offset)
    {
        try
        {
            return Read(expression, offset, true, allowExpressionConstants: false);
        }
        catch (ErlangException)
        {
            throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.InvalidAttributeLiteral, offset);
        }
    }

    private static Term Read(Expr expression, int offset, bool functionArity, bool allowExpressionConstants, IReadOnlyDictionary<string, Term>? bindings = null) => expression switch
    {
        Expr.Literal literal => literal.Value,
        Expr.Variable variable when bindings is not null && bindings.TryGetValue(variable.Name, out Term? value) => value,
        Expr.Tuple tuple => new TupleTerm(tuple.Items.Select(item => Read(item, offset, functionArity, allowExpressionConstants, bindings)).ToArray()),
        Expr.RecordConstruction record when allowExpressionConstants
            && (record.Wildcard is null || TryNormalizeConstant(record.Wildcard, out _, bindings))
            => Read(RecordExpressions.Create(record), offset, functionArity, allowExpressionConstants, bindings),
        Expr.List list => Cons.From(list.Items.Select(item => Read(item, offset, functionArity, allowExpressionConstants, bindings)), list.Tail is null ? null : Read(list.Tail, offset, functionArity, allowExpressionConstants, bindings)),
        Expr.Map { Base: null } map when map.Fields.All(field => !field.Exact) => new MapTerm(map.Fields.Select(field =>
            new KeyValuePair<Term, Term>(Read(field.Key, offset, false, allowExpressionConstants, bindings), Read(field.Value, offset, functionArity, allowExpressionConstants, bindings)))),
        Expr.Bits bits => BitConstruction.Create(
            bits.Segments.Select(segment =>
            (Read(segment.Value, offset, false, allowExpressionConstants, bindings), segment.Size is null ? null : Read(segment.Size, offset, false, allowExpressionConstants, bindings), segment)).ToArray(),
            true
        ),
        Expr.Unary { Operator: ErlangOperators.Plus, Operand: Expr.Literal { Value: Integer or FloatTerm } literal } => literal.Value,
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: Integer integer } } => new Integer(-integer.Value),
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: FloatTerm floating } } => new FloatTerm(-floating.Value),
        Expr.Binary { Operator: ErlangOperators.Divide, Left: Expr.Literal { Value: Atom name }, Right: Expr.Literal { Value: Integer arity } } when functionArity => Term.Tuple(name, arity),
        Expr.Binary { Operator: ErlangOperators.Append } append when allowExpressionConstants => Cons.From(
            Cons.Items(Read(append.Left, offset, false, true, bindings)),
            Read(append.Right, offset, false, true, bindings)
        ),
        Expr.Unary or Expr.Binary when allowExpressionConstants && PatternArithmetic.Evaluate(expression) is { } value => value,
        _ => throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.InvalidAttributeLiteral, offset)
    };
}
