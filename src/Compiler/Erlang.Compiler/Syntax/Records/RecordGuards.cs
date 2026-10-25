namespace Erlang.Compiler;

internal static class RecordGuards
{
    private sealed record Normalized(Expr Value);
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Expr, Normalized> normalized = new();

    public static Expr Normalize(Expr expression) => normalized.GetValue(expression, value => new(Lower(value))).Value;

    private static Expr Lower(Expr expression)
    {
        if (expression is Expr.GuardAlternatives alternatives)
            return new Expr.GuardAlternatives(alternatives.Items.Select(Normalize).ToArray());

        return Wrap(Expand(expression));
    }

    private static bool BooleanBoundary(string operation) => operation is ErlangOperators.AndAlso or ErlangOperators.OrElse or ErlangOperators.And or ErlangOperators.Or;

    private static Expr Expand(Expr expression) => expression switch
    {
        NativeRecordExpression record => record with { Value = record.Value is null ? null : Expand(record.Value) },
        Expr.RecordConstruction record => Expand(RecordExpressions.Create(record)),
        Expr.RecordAccess record => record with { Value = Expand(record.Value) },
        Expr.Binary binary when BooleanBoundary(binary.Operator) => binary with { Left = Wrap(Expand(binary.Left)), Right = Wrap(Expand(binary.Right)) },
        Expr.Binary binary => binary with { Left = Expand(binary.Left), Right = Expand(binary.Right) },
        Expr.Unary unary => unary with { Operand = Expand(unary.Operand) },
        Expr.Tuple tuple => tuple with { Items = tuple.Items.Select(Expand).ToArray() },
        Expr.List list => list with { Items = list.Items.Select(Expand).ToArray(), Tail = list.Tail is null ? null : Expand(list.Tail) },
        Expr.Call call => call with { Arguments = call.Arguments.Select(Expand).ToArray() },
        Expr.Map map => map with { Base = map.Base is null ? null : Expand(map.Base), Fields = map.Fields.Select(field => field with { Key = Expand(field.Key), Value = Expand(field.Value) }).ToArray() },
        Expr.Bits bits => bits with { Segments = bits.Segments.Select(segment => segment with { Value = Expand(segment.Value), Size = segment.Size is null ? null : Expand(segment.Size) }).ToArray() },
        _ => expression
    };

    private static IEnumerable<Expr.RecordTest> Checks(Expr expression)
    {
        if (expression is Expr.Binary boundary && BooleanBoundary(boundary.Operator))
            yield break;
        IEnumerable<Expr> children = expression switch
        {
            NativeRecordExpression record => record.Children,
            Expr.RecordAccess record => new[] { record.Value },
            Expr.Binary binary => new[] { binary.Left, binary.Right },
            Expr.Unary unary => new[] { unary.Operand },
            Expr.Tuple tuple => tuple.Items,
            Expr.List list => list.Tail is null ? list.Items : list.Items.Append(list.Tail),
            Expr.Call call => call.Arguments,
            Expr.Map map => (map.Base is null ? Enumerable.Empty<Expr>() : new[] { map.Base }).Concat(map.Fields.SelectMany(field => new[] { field.Key, field.Value })),
            Expr.Bits bits => bits.Segments.SelectMany(segment => segment.Size is null ? new[] { segment.Value } : new[] { segment.Value, segment.Size }),
            _ => []
        };
        foreach (var child in children)
            foreach (var check in Checks(child))
                yield return check;
        if (expression is Expr.RecordAccess access)
            yield return new(access.Value, access.Name, access.Arity);
    }

    private static Expr Wrap(Expr expression)
    {
        Expr result = expression;
        foreach (var check in Checks(expression).Reverse())
            result = new Expr.Binary(ErlangOperators.AndAlso, check, result);

        return result;
    }
}
