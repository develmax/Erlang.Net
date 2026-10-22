namespace Erlang.Compiler;

internal static class NamedFunctionReferences
{
    public static FunctionTerm Evaluate(ModuleDefinition? module, Expr.FunctionReference reference, ProcessContext context)
    {
        if (module is not null && module.Functions.Any(function => function.Name == reference.Name && function.Arity == reference.Arity))
        {
            var identity = LocalFunctionEntries.GetNamed(module, reference.Name, reference.Arity);
            var image = context.ExecutingSourceImage(module) ?? context.Runtime.Modules.SourceImage(module.Name, module);

            SourceStoredFunctionRecipe recipe = SourceStoredFunctionRecipe.Register(
                context.Runtime.Functions,
                module,
                identity,
                null,
                reference.Name,
                [],
                image,
                reference.Arity
            );

            return recipe.Create([]);
        }
        string? target = SourceCallResolution.ExternalModule(module, reference.Name, reference.Arity);
        if (target is null)
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);

        return context.Runtime.Functions.External(
            context.Runtime,
            new ExternalFunctionIdentity(Term.A(target), Term.A(reference.Name), reference.Arity)
        );
    }
}
