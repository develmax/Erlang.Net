namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private void OrderStandaloneComprehensionsWithClosures(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyList<(FunctionDefinition Function, BeamSourceLocation Location)> helpers
    )
    {
        if (ModuleOptimizationDisabled || helpers.Count == 0
            || !hasMixedRecursiveDefinitions && deferredProjections.Count != 0
            || !functions.Any(function => function.Clauses.Any(clause =>
                LocalFunctionEntries.Walk(clause.Body).Any(expression => expression is Expr.Fun))))
        {
            return;
        }

        var parents = functions.ToDictionary(function => MappingName(function.Name, function.Arity), StringComparer.Ordinal);
        foreach (FunctionDefinition function in functions)
        {
            if (writableBinaryListOwners.TryGetValue((function.Name, function.Arity), out var plan))
            {
                parents.Add(plan.HelperName, function);
            }
        }

        var standalone = new List<(FunctionDefinition Parent, FunctionDefinition Helper)>();
        var positioned = hasMixedRecursiveDefinitions
            ? recursiveDefinitionOrder.Values.SelectMany(order => order)
                .Where(definition => definition.Arity.HasValue)
                .Select(definition => (definition.Name, definition.Arity!.Value))
                .ToHashSet()
            : null;
        foreach (var (helper, _) in helpers)
        {
            if (positioned?.Contains((helper.Name, helper.Arity)) is true)
            {
                continue;
            }

            if (!parents.TryGetValue(helper.Name, out var parent))
            {
                return;
            }

            if (writableBinaryListHelpers.ContainsKey((helper.Name, helper.Arity))
                || HasMapIteratorHelper((helper.Name, helper.Arity))
                || emptyComprehensionScans.Contains((parent.Name, parent.Arity))
                || helper.IsRecursiveListMapper
                    && recursiveDefinitionOrder.TryGetValue((parent.Name, parent.Arity), out var definitions)
                    && definitions is [var definition] && definition == (helper.Name, helper.Arity))
            {
                standalone.Add((parent, helper));
                continue;
            }

            if (parent.Clauses is not
                [
                {
                    Body: Expr.Sequence
                    {
                        Items:
                                [
                                    Expr.Match { Pattern: Pattern.Variable bound },
                                    Expr.ListComprehension
                                {
                                    Items: [var mapped],
                                    Qualifiers:
                                            [
                                                ComprehensionQualifier.Generator
                                            {
                                                Strict: false, Pattern: Pattern.Variable, Source: Expr.Variable source
                                            }
                                            ]
                                }
                                ]
                    }
                }
                ]
                || source.Name != bound.Name || LocalFunctionEntries.Walk(mapped).Any(expression =>
                    expression is Expr.Fun or Expr.ListComprehension or Expr.MapComprehension or Expr.BinaryComprehension))
            {
                return;
            }

            standalone.Add((parent, helper));
        }

        // Core-to-SSA prepends both standalone letrec helpers and anonymous functions as it visits declared functions.
        // Use the same closure boundaries for their shared reverse definition order.
        foreach (var (parent, helper) in standalone)
        {
            recursiveDefinitionOrder.TryAdd((parent.Name, parent.Arity), [(helper.Name, helper.Arity)]);
        }

        hasMixedRecursiveDefinitions = true;
    }
}
