namespace Erlang.Compiler;

internal static class SourceBifContinuationBindings
{
    internal static void Bind(ModuleDefinition module)
    {
        var allowed = new HashSet<Expr.Call>(ReferenceEqualityComparer.Instance);
        var blocked = new HashSet<Expr.Call>(ReferenceEqualityComparer.Instance);
        VisitClauses(module.Functions.SelectMany(function => function.Clauses), true);
        foreach (var call in allowed.Concat(blocked))
            call.CanDiscardBifContinuation = allowed.Contains(call) && !blocked.Contains(call);

        void VisitClauses(IEnumerable<Clause> clauses, bool permitted)
        {
            foreach (var clause in clauses)
            {
                // Pattern expressions and guards are not function return continuations.
                foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]))
                    if (!ReferenceEquals(expression, clause.Body))
                        Visit(expression, false);
                Visit(clause.Body, permitted);
            }
        }

        void Visit(Expr expression, bool permitted)
        {
            switch (expression)
            {
                case Expr.Call call:
                    (permitted && (SourceBifReturnContracts.IsCandidate(module, call) || SourceBifForwardingContracts.Resolve(module, call, out _, out _) is not null) ? allowed : blocked).Add(call);
                    break;
                case Expr.Fun function:
                    VisitClauses(function.Clauses, true);
                    return;
                case Expr.Try tried:
                    // Only the expression before "of" is caught by this handler.
                    // An after clause requires separate CFG proof; decline the entire region.
                    bool bodyPermitted = permitted && tried.After is null;
                    Visit(tried.Body, false);
                    VisitClauses(tried.Clauses, bodyPermitted);
                    VisitClauses(tried.Catches, bodyPermitted);
                    if (tried.After is { } cleanup)
                        Visit(cleanup, false);
                    return;
                case Expr.Catch caught:
                    Visit(caught.Operand, false);
                    return;
                case Expr.Match matched:
                    foreach (var child in LocalFunctionEntries.Children(matched))
                        Visit(child, ReferenceEquals(child, matched.Value) && permitted);
                    return;
                case Expr.MaybeMatch maybeMatched:
                    foreach (var child in LocalFunctionEntries.Children(maybeMatched))
                        Visit(child, ReferenceEquals(child, maybeMatched.Value) && permitted);
                    return;
                case Expr.Maybe maybe:
                    foreach (var item in maybe.Items)
                        Visit(item, permitted);
                    VisitClauses(maybe.Clauses, permitted);
                    return;
                case Expr.Case conditional:
                    Visit(conditional.Value, permitted);
                    VisitClauses(conditional.Clauses, permitted);
                    return;
                case Expr.If conditionalIf:
                    VisitClauses(conditionalIf.Clauses, permitted);
                    return;
                case Expr.Receive receive:
                    VisitClauses(receive.Clauses, permitted);
                    if (receive.Timeout is { } timeout)
                        Visit(timeout, permitted);
                    if (receive.After is { } after)
                        Visit(after, permitted);
                    return;
                case Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension:
                    // Generated helpers have their own return boundaries; no outer-frame transfer.
                    foreach (var child in LocalFunctionEntries.Children(expression))
                        Visit(child, false);
                    return;
            }
            foreach (var child in LocalFunctionEntries.Children(expression))
                Visit(child, permitted);
        }
    }
}
