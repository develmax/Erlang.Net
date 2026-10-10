// Modified: CLR adaptation of OTP-29.1.1 erl_eval maybe_match_exprs/4 and maybe clauses.
// Private scopes preserve the no-export contract; compiled AST execution reuses CLR constraints.
namespace Erlang.Compiler;

internal static class MaybeExpressionExecution
{
    public static async ValueTask<Term> Evaluate(
        Expr.Maybe expression,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        Func<Pattern, Term, Dictionary<string, Term>, bool> match,
        Func<Clause, Term, Dictionary<string, Term>, bool> matchClause
    )
    {
        var body = CompiledBindingScope.Copy(bindings);
        Term value = Nil.Value;
        foreach (var item in expression.Items)
        {
            if (item is not Expr.MaybeMatch conditional)
            {
                value = await evaluate(item, body);
                continue;
            }
            value = await evaluate(conditional.Value, body);
            var candidate = CompiledBindingScope.Copy(body);
            if (match(conditional.Pattern, value, candidate))
            {
                body = candidate;
                continue;
            }
            if (expression.Clauses.Count == 0)
                return value;
            foreach (var clause in expression.Clauses)
            {
                var scope = CompiledBindingScope.Copy(bindings);
                if (matchClause(clause, value, scope))
                    return await evaluate(clause.Body, scope);
            }

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.ElseClause), value));
        }

        return value;
    }
}
