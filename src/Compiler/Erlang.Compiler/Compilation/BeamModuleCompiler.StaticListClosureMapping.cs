namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<string, int> deferredListClosureTargets = new(StringComparer.Ordinal);

    private static Expr.Fun? ReadClosedListClosure(
        Clause clause,
        Expr.ListComprehension comprehension,
        string name
    )
    {
        if (ReadDominatingComprehensionValue(clause, comprehension, name) is not Expr.Fun { SelfName: null } closure
            || closure.Clauses is not [.., { Guard: null, Patterns: [Pattern.Variable] }]
            || closure.Clauses.Any(candidate => candidate.Patterns.Count != 1)
            || ClosureCaptures.ReadListComprehensionBindings(clause, comprehension) is not { } visible)
        {
            return null;
        }

        // Match the lifted fun's lexical capture proof across all its clauses.
        var bindings = visible.ToDictionary(variable => variable, _ => (Term)Nil.Value, StringComparer.Ordinal);

        return ClosureCaptures.Capture(closure, bindings).Count == 0 ? closure : null;
    }

    private static bool ClauseBindsName(IReadOnlyList<Pattern> patterns, string name) =>
        patterns.SelectMany(Semantics.Variables).Contains(name, StringComparer.Ordinal);

    private int DeferListClosureTarget(string name)
    {
        if (!deferredListClosureTargets.TryGetValue(name, out int target))
        {
            // Ordinary deferred function targets use negated definition indexes.
            target = int.MinValue + 1 + deferredListClosureTargets.Count;
            deferredListClosureTargets.Add(name, target);
        }

        return target;
    }

    private void ResolveListClosureTargets()
    {
        foreach (var (name, target) in deferredListClosureTargets)
        {
            var closure = closures.Single(closure => closure.Name == name && closure.Arity == 1 && closure.CaptureCount == 0);
            deferredEntries.Add(target, closure.Entry);
        }
    }
}
