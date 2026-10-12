namespace Erlang.Compiler;

internal static class PredefinedModuleFunctions
{
    public static IReadOnlyList<FunctionDefinition> Add(string module, IReadOnlyList<FunctionDefinition> functions)
    {
        var owner = new Expr.Literal(Term.A(module));
        FunctionDefinition[] predefined =
        [
            new(
                FunctionResolutionNames.ModuleInfo,
                0,
                [new Clause([], null, new Expr.Call(ErlangModuleNames.Module, ErlangModuleNames.GetModuleInformation, [owner]))]
            ),
            new(
                FunctionResolutionNames.ModuleInfo,
                1,
                [new Clause(
                    [new Pattern.Variable(PredefinedModuleFunctionNames.KeyParameter)],
                    null,
                    new Expr.Call(
                        ErlangModuleNames.Module,
                        ErlangModuleNames.GetModuleInformation,
                        [owner, new Expr.Variable(PredefinedModuleFunctionNames.KeyParameter)]
                    )
                )]
            )
        ];

        return Array.AsReadOnly(functions.Concat(predefined).ToArray());
    }

    public static IReadOnlyList<(string Name, int Arity)> Exports(IReadOnlyList<(string Name, int Arity)> exports)
    {
        return Array.AsReadOnly(exports.Concat(new[] { (FunctionResolutionNames.ModuleInfo, 0), (FunctionResolutionNames.ModuleInfo, 1) }.Where(signature => !exports.Contains(signature))).ToArray());
    }
}
