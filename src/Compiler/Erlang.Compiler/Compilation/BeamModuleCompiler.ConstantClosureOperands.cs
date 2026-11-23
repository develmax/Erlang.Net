namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static Dictionary<Expr.Fun, Expr.Fun> FoldImmutableClosureOperands(
        IReadOnlyList<Clause> clauses
    )
    {
        var replacements = new Dictionary<Expr.Fun, Expr.Fun>(ReferenceEqualityComparer.Instance);
        foreach (var clause in clauses)
        {
            if (clause.Body is not Expr.Sequence sequence)
            {
                continue;
            }

            var names = clause.Patterns.SelectMany(Semantics.Variables).ToHashSet(StringComparer.Ordinal);
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            foreach (Expr statement in sequence.Items)
            {
                var match = statement as Expr.Match;
                Expr value = match?.Value ?? statement;
                if (value is Expr.Fun closure && constants.Count != 0
                    && FoldClosure(closure, constants, names) is { } folded)
                {
                    replacements.Add(closure, folded);
                }

                // A nested match or branch may bind more variables in this scope.
                if (value is not Expr.Fun && LocalFunctionEntries.Walk(value).Skip(1).Any(item =>
                        item is Expr.Match or Expr.MaybeMatch or Expr.Case or Expr.If or Expr.Receive or Expr.Try or Expr.Maybe)
                    || value is Expr.Case or Expr.If or Expr.Receive or Expr.Try or Expr.Maybe)
                {
                    break;
                }

                if (match is null)
                {
                    continue;
                }

                if (match.Pattern is Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding
                    && !names.Contains(binding.Name) && AttributeLiteral.TryNormalizeConstant(value, out Term literal, constants))
                {
                    constants.Add(binding.Name, literal);
                }

                names.UnionWith(Semantics.Variables(match.Pattern));
            }
        }

        return replacements;
    }

    private static Expr.Fun? FoldClosure(
        Expr.Fun closure,
        IReadOnlyDictionary<string, Term> constants,
        IReadOnlySet<string> names
    )
    {
        var captured = ClosureCaptures.Capture(closure, constants);
        if (captured.Count == 0)
        {
            return null;
        }

        var expressions = LocalFunctionEntries.ClauseExpressions(closure.Clauses).SelectMany(LocalFunctionEntries.Walk).ToArray();
        if (expressions.Any(expression => expression is Expr.Fun or Expr.ListComprehension or Expr.MapComprehension
            or Expr.BinaryComprehension or Expr.Try or Expr.Receive or Expr.Maybe))
        {
            return null;
        }

        var patterns = closure.Clauses.SelectMany(clause => clause.Patterns).Concat(expressions.SelectMany<Expr, Pattern>(expression =>
            expression switch
            {
                Expr.Match match => [match.Pattern],
                Expr.Case branch => branch.Clauses.SelectMany(clause => clause.Patterns),
                _ => []
            }));
        foreach (string name in patterns.SelectMany(Semantics.Variables))
        {
            captured.Remove(name);
        }

        if (captured.Count == 0)
        {
            return null;
        }

        if (!closure.Clauses.Any(clause => clause.Guard is null && HasUnconditionalParameters(clause)))
        {
            var bindings = names.ToDictionary(name => name, _ => (Term)Nil.Value, StringComparer.Ordinal);
            if (ClosureCaptures.Capture(closure, bindings).Keys.All(captured.ContainsKey))
            {
                // Removing every capture changes the existing lifted failure entry.
                return null;
            }
        }

        // A closed immutable value is already known at the point where the fun is formed.
        // Keep lexical pattern bindings in the original AST and replace only operand reads.
        return (Expr.Fun)BeamTargetedInlining.Rewrite(
            closure,
            expression => expression is Expr.Variable variable && captured.TryGetValue(variable.Name, out var value)
                ? new Expr.Literal(value) : expression
        );
    }
}
