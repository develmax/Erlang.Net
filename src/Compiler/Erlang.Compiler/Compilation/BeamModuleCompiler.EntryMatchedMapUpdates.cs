namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private IReadOnlySet<Expr.Map> entryMatchedMapUpdates = new HashSet<Expr.Map>(ReferenceEqualityComparer.Instance);

    // A measured body and the emitted body must use the same immutable analysis facts.
    private BeamModuleCompiler CreateLayoutProbe() => new(name, sourceModule: sourceModule)
    {
        exportedFunctions = exportedFunctions,
        entryMatchedMapUpdates = entryMatchedMapUpdates
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
                if (known.Count > 0)
                {
                    FindUpdates(clause.Body, known, updates);
                }
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

        if (expression is Expr.Map { Base: Expr.Variable variable } map && known.Contains(variable.Name))
        {
            updates.Add(map);
        }

        foreach (Expr child in LocalFunctionEntries.Children(expression))
        {
            FindUpdates(child, known, updates);
        }
    }

    private static string? MapOwner(Pattern pattern) => pattern switch
    {
        AliasPattern { Left: MapPattern, Right: Pattern.Variable { Name: not VariableScopeNames.Wildcard } name } => name.Name,
        AliasPattern { Left: Pattern.Variable { Name: not VariableScopeNames.Wildcard } name, Right: MapPattern } => name.Name,
        _ => null
    };
}
