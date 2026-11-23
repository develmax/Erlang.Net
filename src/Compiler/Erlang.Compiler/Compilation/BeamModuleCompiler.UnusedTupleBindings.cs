namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition FoldUnusedTupleBindings(FunctionDefinition function)
    {
        if (ModuleOptimizationDisabled)
        {
            return function;
        }

        return function with { Clauses = function.Clauses.Select(FoldClause).ToArray() };

        static Clause FoldClause(Clause clause)
        {
            Expr[] body = LocalFunctionEntries.Walk(clause.Body).ToArray();
            Expr.Match[] matches = body.OfType<Expr.Tuple>()
                .SelectMany(tuple => tuple.Items.OfType<Expr.Match>())
                .Where(match => match.Pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard })
                .ToArray();
            if (matches.Length == 0 || body.Any(expression => expression is
                Expr.Case or Expr.If or Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe or Expr.Fun
                or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension))
            {
                return clause;
            }

            // A second pattern occurrence may compare against the earlier binding.
            // Treat every occurrence as a use unless it is the binding itself.
            var names = LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk)
                .OfType<Expr.Variable>().Select(variable => variable.Name)
                .Concat(clause.Patterns.SelectMany(Semantics.Variables))
                .Concat(body.OfType<Expr.Match>().SelectMany(match => Semantics.Variables(match.Pattern)));
            var counts = names.GroupBy(name => name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var literals = new Dictionary<string, Term>(StringComparer.Ordinal);
            foreach (Expr.Match match in matches)
            {
                string name = ((Pattern.Variable)match.Pattern).Name;
                if (counts[name] == 1 && AttributeLiteral.TryNormalizeConstant(match.Value, out Term value))
                {
                    literals.Add(name, value);
                }
            }

            if (literals.Count == 0)
            {
                return clause;
            }

            return clause with
            {
                Body = BeamTargetedInlining.Rewrite(
                    clause.Body,
                    expression =>
                    expression is Expr.Match { Pattern: Pattern.Variable variable }
                    && literals.TryGetValue(variable.Name, out Term? literal)
                        ? new Expr.Literal(literal)
                        : expression
                )
            };
        }
    }
}
