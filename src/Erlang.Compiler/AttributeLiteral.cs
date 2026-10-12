namespace Erlang.Compiler;

internal static class AttributeLiteral
{
    public static Term Normalize(Expr expression, int offset)
    {
        try
        {
            return Read(expression, offset, true);
        }
        catch (ErlangException)
        {
            throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.InvalidAttributeLiteral, offset);
        }
    }

    private static Term Read(Expr expression, int offset, bool functionArity) => expression switch
    {
        Expr.Literal literal => literal.Value,
        Expr.Tuple tuple => new TupleTerm(tuple.Items.Select(item => Read(item, offset, functionArity)).ToArray()),
        Expr.List list => Cons.From(list.Items.Select(item => Read(item, offset, functionArity)), list.Tail is null ? null : Read(list.Tail, offset, functionArity)),
        Expr.Map { Base: null } map when map.Fields.All(field => !field.Exact) => new MapTerm(map.Fields.Select(field =>
            new KeyValuePair<Term, Term>(Read(field.Key, offset, false), Read(field.Value, offset, functionArity)))),
        Expr.Bits bits => BitConstruction.Create(
            bits.Segments.Select(segment =>
            (Read(segment.Value, offset, false), segment.Size is null ? null : Read(segment.Size, offset, false), segment)).ToArray(),
            true
        ),
        Expr.Unary { Operator: ErlangOperators.Plus, Operand: Expr.Literal { Value: Integer or FloatTerm } literal } => literal.Value,
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: Integer integer } } => new Integer(-integer.Value),
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: FloatTerm floating } } => new FloatTerm(-floating.Value),
        Expr.Binary { Operator: ErlangOperators.Divide, Left: Expr.Literal { Value: Atom name }, Right: Expr.Literal { Value: Integer arity } } when functionArity => Term.Tuple(name, arity),
        _ => throw new CompileException(CompilerDiagnosticCodes.Syntax, ParserDiagnostics.InvalidAttributeLiteral, offset)
    };
}
