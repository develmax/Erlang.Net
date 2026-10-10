// Modified: C# adaptation of OTP-29.1.1 erl_eval map/ eval_map_fields/merge_bindings
// (ordered-dictionary branch); compiled route follows v3_core map pre-expression concatenation.
// Uses async callbacks, CLR collections and the current name-only lint model.
// See docs/OTP-PORTS.md for exact provenance and remaining deviations.

namespace Erlang.Compiler;

internal static class MapExpressionBindings
{
    public static async ValueTask<Term> Evaluate(
        Expr.Map map,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        Func<Term, IReadOnlyList<(Term Key, Term Value, bool Exact)>, MapTerm> build,
        bool compiled
    )
    {
        var fields = new (Term Key, Term Value, bool Exact)[map.Fields.Count];
        var baseScope = CompiledBindingScope.Copy(bindings);
        var fieldScope = compiled ? baseScope : CompiledBindingScope.Copy(bindings);
        Term mapBase = map.Base is null ? new MapTerm([]) : await evaluate(map.Base, baseScope);
        for (int i = 0; i < fields.Length; i++)
        {
            var field = map.Fields[i];
            var key = await evaluate(field.Key, fieldScope);
            var value = await evaluate(field.Value, fieldScope);
            fields[i] = (key, value, field.Exact);
        }
        // OTP validates/materializes the map before merging base and field bindings.
        var result = build(mapBase, fields);
        var merged = compiled || map.Base is null ? fieldScope : ExpressionBindings.Merge(fieldScope, baseScope);
        foreach (var binding in merged)
            bindings[binding.Key] = binding.Value;

        return result;
    }

    public static IEnumerable<Expr> Expressions(Expr.Map map)
    {
        if (map.Base is not null)
            yield return map.Base;
        foreach (var field in map.Fields)
        {
            yield return field.Key;
            yield return field.Value;
        }
    }
}
