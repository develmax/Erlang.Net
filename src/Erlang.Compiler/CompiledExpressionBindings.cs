// Modified: CLR subset adapter for OTP-29.1.1 v3_core safe_list,
// ulinearize_exprs and uexprs match failures, using the erl_eval binding helper.
// Retains independent child environments (including closure capture), checks
// each returned child's bindings in source order, and reports its new value.
// Modified: known-variable constraints now check explicit patterns at their
// match point, preserving the full RHS value and stopping following effects.
// This is not a port of full Core Erlang lowering or optimization.

namespace Erlang.Compiler;

internal static class CompiledExpressionBindings
{
    public static async ValueTask<Term[]> EvaluateList(
        IReadOnlyList<Expr> expressions,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var original = CompiledBindingScope.Copy(bindings);
        var merged = new Dictionary<string, Term>(original, StringComparer.Ordinal);
        var values = new Term[expressions.Count];
        for (int i = 0; i < expressions.Count; i++)
        {
            var scope = new CompiledBindingScope(original, merged);
            values[i] = await evaluate(expressions[i], scope);
            merged = ExpressionBindings.Merge(merged, scope);
        }
        foreach (var binding in merged)
            bindings[binding.Key] = binding.Value;

        return values;
    }

    public static async ValueTask<Term> EvaluateCons(
        Expr.List list,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var expressions = list.Tail is null ? list.Items : list.Items.Append(list.Tail).ToArray();
        var values = await EvaluateList(expressions, bindings, evaluate);

        return Cons.From(values.Take(list.Items.Count), list.Tail is null ? Nil.Value : values[^1]);
    }
}
