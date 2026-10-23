namespace Erlang.Compiler;

internal static class NativeRecordDefault
{
    public static Term Read(Expr expression, int offset) => expression switch
    {
        Expr.Literal literal => literal.Value,
        Expr.Tuple tuple => new TupleTerm(tuple.Items.Select(item => Read(item, offset))),
        Expr.List list => Cons.From(list.Items.Select(item => Read(item, offset)), list.Tail is null ? null : Read(list.Tail, offset)),
        Expr.Map { Base: null } map when map.Fields.All(field => !field.Exact) => new MapTerm(map.Fields.Select(field => new KeyValuePair<Term, Term>(Read(field.Key, offset), Read(field.Value, offset)))),
        Expr.Binary or Expr.Unary => Scalar(Evaluate(expression, offset), offset),
        Expr.Bits bits when bits.Segments.All(segment => segment.IsStringLiteral || segment.SourceStringLiteral) => AttributeLiteral.Normalize(bits, offset),
        _ => throw Invalid(offset)
    };

    private static Term Evaluate(Expr expression, int offset) => expression switch
    {
        Expr.Literal literal when literal.Value is Integer or FloatTerm or Atom or Nil => literal.Value,
        Expr.Tuple tuple => new TupleTerm(tuple.Items.Select(item => Evaluate(item, offset))),
        Expr.List list => Cons.From(list.Items.Select(item => Evaluate(item, offset)), list.Tail is null ? null : Evaluate(list.Tail, offset)),
        Expr.Unary unary => ErlangOperatorValues.Unary(unary.Operator, Evaluate(unary.Operand, offset)),
        Expr.Binary binary when binary.Operator is not (ErlangOperators.AndAlso or ErlangOperators.OrElse or ErlangOperators.Send) => ErlangOperatorValues.Binary(
            binary.Operator,
            Evaluate(binary.Left, offset),
            Evaluate(binary.Right, offset),
            null
        ),
        _ => throw Invalid(offset)
    };

    private static Term Scalar(Term value, int offset) => value is Integer or FloatTerm or Atom ? value : throw Invalid(offset);

    private static CompileException Invalid(int offset) => new(CompilerDiagnosticCodes.RecordDefinition, NativeRecordDiagnostics.InvalidDefault(string.Empty, string.Empty), offset);
}
