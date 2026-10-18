namespace Erlang.Compiler;

internal static class SourceContinuationBindings
{
    internal static void Bind(ModuleDefinition module)
    {
        foreach (var function in module.Functions)
        {
            VisitClauses(function.Clauses);
        }
        SourceBifContinuationBindings.Bind(module);
    }

    private static void VisitClauses(IReadOnlyList<Clause> clauses)
    {
        foreach (var expression in LocalFunctionEntries.ClauseExpressions(clauses))
        {
            Visit(expression, false, new HashSet<string>(StringComparer.Ordinal));
        }
    }

    private static void Visit(Expr expression, bool protectedContinuation, HashSet<string> staticTargets)
    {
        switch (expression)
        {
            case Expr.Apply apply:
                {
                    apply.CanDiscardContinuation = !protectedContinuation && IsStaticTarget(apply.Function, staticTargets);
                    break;
                }
            case Expr.Fun function:
                {
                    VisitClauses(function.Clauses);

                    return;
                }
            case Expr.Sequence sequence:
                {
                    foreach (var item in sequence.Items)
                    {
                        Visit(item, protectedContinuation, staticTargets);
                    }

                    return;
                }
            case Expr.Block block:
                {
                    Visit(block.Body, protectedContinuation, staticTargets);

                    return;
                }
            case Expr.Match match:
                {
                    foreach (var child in LocalFunctionEntries.Children(match))
                    {
                        Visit(child, protectedContinuation, Copy(staticTargets));
                    }
                    bool knownTarget = IsStaticTarget(match.Value, staticTargets);
                    foreach (string name in Semantics.Variables(match.Pattern))
                    {
                        staticTargets.Remove(name);
                    }
                    if (knownTarget && match.Pattern is Pattern.Variable variable)
                    {
                        staticTargets.Add(variable.Name);
                    }

                    return;
                }
        }

        bool childProtected = protectedContinuation || expression is Expr.Try or Expr.Catch;
        bool lexicalBarrier = expression is Expr.Case or Expr.If or Expr.Receive or Expr.Try or Expr.Maybe
            or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension;
        foreach (var child in LocalFunctionEntries.Children(expression))
        {
            Visit(child, childProtected, lexicalBarrier ? new HashSet<string>(StringComparer.Ordinal) : Copy(staticTargets));
        }
    }

    private static bool IsStaticTarget(Expr expression, HashSet<string> staticTargets) => expression switch
    {
        Expr.FunctionReference or Expr.Fun => true,
        Expr.Variable variable => staticTargets.Contains(variable.Name),
        _ => false
    };

    private static HashSet<string> Copy(HashSet<string> source) => new(source, StringComparer.Ordinal);
}
