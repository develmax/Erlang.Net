// Modified: current-AST scheduling subset of OTP-29.1.1 v3_core:mc_pairs/mc_tuples.
// Preserve the from_keys prefix and first fallback pre-expression ordering.
namespace Erlang.Compiler;

internal static class MapComprehensionBindings
{
    public static async ValueTask<IReadOnlyList<KeyValuePair<Term, Term>>> Evaluate(
        IReadOnlyList<MapField> fields,
        IReadOnlyList<ComprehensionQualifier> qualifiers,
        IReadOnlyDictionary<string, Term> incoming,
        Dictionary<string, Term> scope,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var bound = new HashSet<string>(scope.Keys.Except(incoming.Keys), StringComparer.Ordinal);
        foreach (var qualifier in qualifiers)
        {
            Pattern? pattern = qualifier switch
            {
                ComprehensionQualifier.Generator generator => generator.Pattern,
                ComprehensionQualifier.BinaryGenerator generator => generator.Pattern,
                ComprehensionQualifier.MapGenerator generator => generator.Pattern,
                _ => null
            };
            if (pattern is not null)
                bound.UnionWith(Semantics.Variables(pattern));
            if (qualifier is ComprehensionQualifier.Filter filter)
                bound.UnionWith(FilterBindings(filter.Expression));
        }
        int prefix = 0;
        while (prefix < fields.Count && IsSafe(fields[prefix].Value, bound) && (prefix == 0 || Same(fields[0].Value, fields[prefix].Value)))
            prefix++;
        var expressions = new List<Expr>();
        var keyPositions = new int[fields.Count];
        var valuePositions = new int[fields.Count];
        if (prefix < fields.Count)
        {
            Value(prefix);
            Key(prefix);
        }
        for (int index = prefix - 1; index >= 0; index--)
            Key(index);
        for (int index = prefix + 1; index < fields.Count; index++)
        {
            Key(index);
            Value(index);
        }
        var values = await CompiledExpressionBindings.EvaluateList(expressions, CompiledBindingScope.Copy(scope), evaluate);
        var result = new KeyValuePair<Term, Term>[fields.Count];
        for (int index = 0; index < fields.Count; index++)
        {
            Term value = index < prefix ? await evaluate(fields[index].Value, CompiledBindingScope.Copy(scope)) : values[valuePositions[index]];
            result[index] = new(values[keyPositions[index]], value);
        }

        return result;

        void Key(int index)
        {
            keyPositions[index] = expressions.Count;
            expressions.Add(fields[index].Key);
        }

        void Value(int index)
        {
            valuePositions[index] = expressions.Count;
            expressions.Add(fields[index].Value);
        }
    }

    private static IEnumerable<string> FilterBindings(Expr expression) => expression switch
    {
        Expr.Match match => Semantics.Variables(match.Pattern).Concat(FilterBindings(match.Value)),
        Expr.Block block => FilterBindings(block.Body),
        Expr.Sequence sequence => sequence.Items.SelectMany(FilterBindings),
        Expr.Tuple tuple => tuple.Items.SelectMany(FilterBindings),
        Expr.List list => list.Items.SelectMany(FilterBindings).Concat(list.Tail is null ? [] : FilterBindings(list.Tail)),
        Expr.Call call => call.Arguments.SelectMany(FilterBindings),
        Expr.Binary binary => FilterBindings(binary.Left).Concat(FilterBindings(binary.Right)),
        Expr.Unary unary => FilterBindings(unary.Operand),
        _ => []
    };

    private static bool IsSafe(Expr expression, HashSet<string> bound) => expression switch
    {
        Expr.Literal => true,
        Expr.Variable variable => !bound.Contains(variable.Name),
        Expr.Tuple tuple => tuple.Items.All(item => IsSafe(item, bound)),
        Expr.List list => list.Items.All(item => IsSafe(item, bound)) && (list.Tail is null || IsSafe(list.Tail, bound)),
        _ => false
    };

    private static bool Same(Expr first, Expr second) => (first, second) switch
    {
        (Expr.Literal left, Expr.Literal right) => left.Value.Equals(right.Value),
        (Expr.Variable left, Expr.Variable right) => left.Name == right.Name,
        (Expr.Tuple left, Expr.Tuple right) => left.Items.Count == right.Items.Count && left.Items.Zip(right.Items).All(pair => Same(pair.First, pair.Second)),
        (Expr.List left, Expr.List right) => left.Items.Count == right.Items.Count && left.Items.Zip(right.Items).All(pair => Same(pair.First, pair.Second)) && (left.Tail is null && right.Tail is null || left.Tail is not null && right.Tail is not null && Same(left.Tail, right.Tail)),
        _ => false
    };
}
