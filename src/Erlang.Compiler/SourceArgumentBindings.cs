namespace Erlang.Compiler;

internal static class SourceArgumentBindings
{
    internal static Expr.List? LiteralList(ModuleDefinition module, FunctionDefinition owner, string variable)
    {
        if (module.Exports.Contains((owner.Name, owner.Arity)) || owner.Clauses.Count != 1)
        {
            return null;
        }
        int parameter = -1;
        for (int index = 0; index < owner.Arity; index++)
        {
            if (owner.Clauses[0].Patterns[index] is Pattern.Variable name && name.Name == variable)
            {
                parameter = index;
            }
        }
        if (parameter < 0)
        {
            return null;
        }
        var elements = new List<Expr>();
        bool called = false;
        foreach (var expression in module.Functions.SelectMany(value => LocalFunctionEntries.ClauseExpressions(value.Clauses)).SelectMany(Walk))
        {
            if (expression is Expr.FunctionReference reference && reference.Name == owner.Name && reference.Arity == owner.Arity)
            {
                return null;
            }
            if (expression is Expr.Call { Module: null } call && call.Function == owner.Name && call.Arguments.Count == owner.Arity)
            {
                if (call.Arguments[parameter] is not Expr.List { Tail: null } list || list.Items.Any(value => value is not Expr.Literal))
                {
                    return null;
                }
                called = true;
                elements.AddRange(list.Items);
                if (elements.Count > ComprehensionReturnParameters.MaximumLiteralItems)
                {
                    return null;
                }
            }
        }

        return called ? new(elements) : null;
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
