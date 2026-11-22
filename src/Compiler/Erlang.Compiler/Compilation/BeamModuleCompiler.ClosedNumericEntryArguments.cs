namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool HasClosedNumericEntryArgument(
        FunctionDefinition target,
        int argument,
        IReadOnlyList<FunctionDefinition> functions
    ) => HasClosedEntryArgument(
        target,
        argument,
        functions,
        static input => input is Expr.Literal { Value: Integer or FloatTerm }
            or Expr.Binary
        {
            Operator: ErlangOperators.Plus or ErlangOperators.Minus
                    or ErlangOperators.Multiply or ErlangOperators.Divide
        }
    );

    private bool HasClosedEntryArgument(
        FunctionDefinition target,
        int argument,
        IReadOnlyList<FunctionDefinition> functions,
        Func<Expr, bool> seed
    )
    {
        var signature = (target.Name, target.Arity);
        if (exportedFunctions.Contains(signature) || IsDeclaredNative(signature)
            || signature == (OnLoadFunction, 0))
        {
            return false;
        }

        bool initialized = false;
        foreach (FunctionDefinition caller in functions)
        {
            foreach (Clause clause in caller.Clauses)
            {
                Expr[] expressions = LocalFunctionEntries.ClauseExpressions([clause])
                    .SelectMany(LocalFunctionEntries.Walk).ToArray();
                bool nested = expressions.Any(expression => expression is Expr.Fun or Expr.ListComprehension
                    or Expr.BinaryComprehension or Expr.MapComprehension);
                foreach (Expr expression in expressions)
                {
                    if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                    {
                        return false;
                    }

                    if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                        || (call.Function, call.Arguments.Count) != signature)
                    {
                        continue;
                    }

                    Expr input = call.Arguments[argument];
                    if (seed(input))
                    {
                        initialized = true;
                        continue;
                    }

                    if (nested || (caller.Name, caller.Arity) != signature
                        || clause.Patterns[argument] is not Pattern.Variable previous
                        || previous.Name == VariableScopeNames.Wildcard
                        || input is not Expr.Variable forwarded || forwarded.Name != previous.Name)
                    {
                        return false;
                    }
                }
            }
        }

        return initialized;
    }
}
