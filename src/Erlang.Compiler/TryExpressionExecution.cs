// Modified: asynchronous CLR adaptation of erl_eval:try_clauses/10.
// Protect only the body, use incoming handler/after scopes and execute after
// outside both success and exception handlers. Stack terms use the existing
// logical subset; this is not a BEAM frame or Core lowering implementation.
namespace Erlang.Compiler;

internal static class TryExpressionExecution
{
    public static async ValueTask<Term> Evaluate(
        Expr.Try expression,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        Func<Clause, Term, Dictionary<string, Term>, bool> match
    )
    {
        var incoming = CompiledBindingScope.Copy(bindings);
        var bodyScope = CompiledBindingScope.Copy(incoming);
        try
        {
            Term value;
            ErlangException? failure = null;
            try
            {
                value = await evaluate(expression.Body, bodyScope);
            }
            catch (ErlangException exception)
            {
                failure = exception;
                value = Term.Tuple(Term.A(exception.ExceptionClass), exception.Reason, exception.StackTraceTerm);
            }
            var clauses = failure is null ? expression.Clauses : expression.Catches;
            var parent = failure is null ? bodyScope : incoming;
            if (failure is null && clauses.Count == 0)
            {
                foreach (var binding in bodyScope)
                    bindings[binding.Key] = binding.Value;

                return value;
            }
            foreach (var clause in clauses)
            {
                var scope = CompiledBindingScope.Copy(parent);
                if (!match(clause, value, scope))
                    continue;
                var result = await evaluate(clause.Body, scope);
                foreach (var binding in scope)
                    bindings[binding.Key] = binding.Value;

                return result;
            }
            if (failure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.TryClause), value));
        }
        finally
        {
            if (expression.After is not null)
                await evaluate(expression.After, CompiledBindingScope.Copy(incoming));
        }
    }
}
