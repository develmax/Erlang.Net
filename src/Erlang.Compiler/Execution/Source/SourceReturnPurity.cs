namespace Erlang.Compiler;

internal static class SourceReturnPurity
{
    internal static bool IsPure(Expr expression) => expression switch
    {
        Expr.Literal or Expr.Variable or Expr.FunctionReference or Expr.Fun => true,
        Expr.Tuple tuple => tuple.Items.All(IsPure),
        Expr.List list => list.Items.All(IsPure) && (list.Tail is null || IsPure(list.Tail)),
        Expr.Unary unary => IsPure(unary.Operand),
        Expr.Binary binary => binary.Operator != ErlangOperators.Send && IsPure(binary.Left) && IsPure(binary.Right),
        Expr.Match { Pattern: Pattern.Variable } match => IsPure(match.Value),
        Expr.Block block => IsPure(block.Body),
        _ => false
    };
}
