namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private IReadOnlySet<Expr.Map> entryMatchedMapUpdates = new HashSet<Expr.Map>(ReferenceEqualityComparer.Instance);

    private BeamModuleCompiler? layoutProbeSource;

    // A measured body and the emitted body must use the same immutable analysis facts.
    private BeamModuleCompiler CreateLayoutProbe() => new(name, sourceModule: sourceModule)
    {
        CoreMapCallsEnabled = CoreMapCallsEnabled,
        exportedFunctions = exportedFunctions,
        entryMatchedMapUpdates = entryMatchedMapUpdates,
        listResultFactSource = listResultFactSource ?? this,
        layoutProbeSource = layoutProbeSource ?? this
    };

    private void IndexEntryMatchedMapUpdates(IReadOnlyList<FunctionDefinition> functions)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled)
        {
            return;
        }

        var updates = new HashSet<Expr.Map>(ReferenceEqualityComparer.Instance);
        foreach (FunctionDefinition function in functions)
        {
            foreach (Clause clause in function.Clauses)
            {
                var known = clause.Patterns.Select(MapOwner).OfType<string>().ToHashSet(StringComparer.Ordinal);
                FindUpdates(clause.Body, known, updates);
            }
        }

        entryMatchedMapUpdates = updates;
    }

    private static void FindUpdates(Expr expression, HashSet<string> known, HashSet<Expr.Map> updates)
    {
        if (expression is Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension
            || expression is NativeRecordExpression)
        {
            // These introduce independently scoped names or backend-specific bindings.
            return;
        }

        if (expression is Expr.Sequence sequence)
        {
            HashSet<string> scoped = known;
            foreach (Expr item in sequence.Items)
            {
                FindUpdates(item, scoped, updates);
                if (item is Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } assignment,
                        Value: var value
                    } && !scoped.Contains(assignment.Name) && IsKnownMapResult(value, scoped))
                {
                    // Only the successful continuation observes this binding.
                    if (ReferenceEquals(scoped, known))
                    {
                        scoped = new HashSet<string>(known, StringComparer.Ordinal);
                    }

                    scoped.Add(assignment.Name);
                }
            }

            return;
        }

        if (expression is Expr.Map { Base: Expr.Variable variable } map && known.Contains(variable.Name))
        {
            updates.Add(map);
        }

        foreach (Expr child in LocalFunctionEntries.Children(expression))
        {
            FindUpdates(child, known, updates);
        }
    }

    private static bool IsKnownMapResult(Expr value, IReadOnlySet<string> known) =>
        value is Expr.Map
        || value is Expr.Case { Value: Expr.Variable or Expr.Literal, Clauses.Count: > 0 } choice
        // A successful case produces a map when each arm returns a map.
        && choice.Clauses.All(arm => arm.Body is Expr.Map
            || arm.Body is Expr.Variable result && known.Contains(result.Name));

    private static string? MapOwner(Pattern pattern) => pattern switch
    {
        AliasPattern { Left: MapPattern, Right: Pattern.Variable { Name: not VariableScopeNames.Wildcard } name } => name.Name,
        AliasPattern { Left: Pattern.Variable { Name: not VariableScopeNames.Wildcard } name, Right: MapPattern } => name.Name,
        _ => null
    };
}
