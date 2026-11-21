namespace Erlang.Compiler;

internal static class BeamSourceReachability
{
    internal static (IReadOnlyList<FunctionDefinition>, IReadOnlyList<BeamSourceLocation>) Retain(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyList<BeamSourceLocation> lines,
        Func<FunctionDefinition, bool> isRoot,
        string? transformedModule = null
    )
    {
        var retained = new bool[functions.Count];
        var pending = new Queue<int>();
        var indices = new Dictionary<(string Name, int Arity), int>();
        for (int index = 0; index < functions.Count; index++)
        {
            FunctionDefinition function = functions[index];
            var signature = (function.Name, function.Arity);
            indices.Add(signature, index);
            if (isRoot(function))
            {
                retained[index] = true;
                pending.Enqueue(index);
            }
        }

        while (pending.TryDequeue(out int caller))
        {
            foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(functions[caller].Clauses)
                .SelectMany(LocalFunctionEntries.Walk))
            {
                if (transformedModule is not null && HasOpaqueDispatch(expression))
                {
                    return (functions, lines);
                }

                var reference = expression switch
                {
                    Expr.Call { Module: null, DynamicRemote: false } local => (local.Function, local.Arguments.Count),
                    Expr.Call { DynamicRemote: false } self when transformedModule is not null && self.Module == transformedModule
                        => (self.Function, self.Arguments.Count),
                    Expr.FunctionReference local => (local.Name, local.Arity),
                    _ => default
                };
                if (reference.Item1 is not null && indices.TryGetValue(reference, out int target) && !retained[target])
                {
                    retained[target] = true;
                    pending.Enqueue(target);
                }
            }
        }

        return (functions.Where((_, index) => retained[index]).ToArray(),
            lines.Where((_, index) => retained[index]).ToArray());
    }

    private static bool HasOpaqueDispatch(Expr expression) => expression is
        Expr.Apply { Function: not (Expr.FunctionReference or Expr.Fun) }
        or Expr.Call { DynamicRemote: true }
        or Expr.Call
        {
            Module: null or ErlangModuleNames.Module,
            Function: AutoImportNames.Apply or FunctionReferenceLoweringNames.MakeFunction
        };
}
