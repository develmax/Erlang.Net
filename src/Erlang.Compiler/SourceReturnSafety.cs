namespace Erlang.Compiler;

internal static class SourceReturnSafety
{
    internal static bool Allows(ModuleDefinition module, string function, int arity)
    {
        if (module.DeclaredNifs?.Contains((function, arity)) == true)
        {
            return false;
        }

        return !module.Functions.SelectMany(value => LocalFunctionEntries.ClauseExpressions(value.Clauses)).SelectMany(Walk)
            .OfType<Expr.Call>().Any(value => value.Function == SourceLoadNifNames.Function
                && value.Arguments.Count == SourceLoadNifNames.Arity
                && value.Module is null or ErlangModuleNames.Module);
    }

    internal static bool HasNativeOverrides(ModuleDefinition module, ProcessContext context)
    {
        var image = context.ExecutingSourceImage(module) ?? context.Runtime.Modules.SourceImage(module.Name, module);

        return image is not null && NifImageBindingStore.TryGetValue(image, out _);
    }

    private static IEnumerable<Expr> Walk(Expr expression)
    {
        yield return expression;
        foreach (var child in LocalFunctionEntries.Children(expression))
        {
            foreach (var nested in Walk(child))
            {
                yield return nested;
            }
        }
    }
}
