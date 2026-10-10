// Modified: CLR list-comprehension subset of OTP-29.1.1 erl_eval/erl_lint.
// Fresh generator bindings, original key/size scopes and filter error distinctions.
namespace Erlang.Compiler;

internal static class ListComprehensionExecution
{
    public static async ValueTask<Term> Evaluate(
        Expr.ListComprehension expression,
        Dictionary<string, Term> incoming,
        ProcessContext context,
        ModuleDefinition? module,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var result = new List<Term>();
        await Qualifiers(0, CompiledBindingScope.Copy(incoming));

        return Cons.From(result);

        async ValueTask Qualifiers(int index, Dictionary<string, Term> scope)
        {
            if (index == expression.Qualifiers.Count)
            {
                var values = module is null
                    ? await ExpressionBindings.EvaluateList(expression.Items, scope, evaluate)
                    : await CompiledExpressionBindings.EvaluateList(expression.Items, scope, evaluate);
                result.AddRange(values);

                return;
            }
            if (expression.Qualifiers[index] is ComprehensionQualifier.Generator generator)
            {
                Term source = await evaluate(generator.Source, CompiledBindingScope.Copy(scope));
                while (source is Cons cell)
                {
                    await context.ReduceAsync();
                    var fresh = new Dictionary<string, Term>(StringComparer.Ordinal);
                    if (generator.Pattern.Match(
                        cell.Head,
                        fresh,
                        context,
                        scope
                    ))
                    {
                        var nested = new Dictionary<string, Term>(scope, StringComparer.Ordinal);
                        foreach (var binding in fresh)
                            nested[binding.Key] = binding.Value;
                        await Qualifiers(index + 1, nested);
                    }
                    else if (generator.Strict)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), cell.Head));
                    source = cell.Tail;
                }
                if (source is not Nil)
                    throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), source));

                return;
            }
            var filter = (ComprehensionQualifier.Filter)expression.Qualifiers[index];
            if (ComprehensionGuard.IsTest(filter.Expression, module))
            {
                if (Execution.Guard(filter.Expression, scope, context))
                    await Qualifiers(index + 1, scope);

                return;
            }
            var filterScope = CompiledBindingScope.Copy(scope);
            var value = await evaluate(filter.Expression, filterScope);
            if (value.Equals(Term.A(ErlangBooleanAtoms.True)))
                await Qualifiers(index + 1, filterScope);
            else if (!value.Equals(Term.A(ErlangBooleanAtoms.False)))
                throw new ErlangException(Term.Tuple(Term.A(ComprehensionErrorReasons.BadFilter), value));
        }
    }
}
