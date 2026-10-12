namespace Erlang.Compiler;

internal static class NamedFunctionReferences
{
    public static FunctionTerm Evaluate(ModuleDefinition? module, Expr.FunctionReference reference, ProcessContext context)
    {
        if (module is not null && module.Functions.Any(function => function.Name == reference.Name && function.Arity == reference.Arity))
        {
            var identity = LocalFunctionEntries.GetNamed(module, reference.Name, reference.Arity);
            var image = context.ExecutingSourceImage(module) ?? context.Runtime.Modules.SourceImage(module.Name, module);

            FunctionTerm Create(IReadOnlyList<Term> environment)
            {
                if (environment.Count != 0)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return new FunctionTerm(
                    identity,
                    reference.Arity,
                    environment,
                    (execution, arguments) => Execution.InvokeAsync(
                        module,
                        reference.Name,
                        (ProcessContext)execution,
                        arguments,
                        image
                    )
                );
            }
            identity = context.Runtime.Functions.Register(
                identity,
                reference.Arity,
                0,
                Create
            );

            return Create([]);
        }
        if (!AutoImportSignatures.Contains(reference.Name, reference.Arity))
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);

        return context.Runtime.Functions.External(
            context.Runtime,
            new ExternalFunctionIdentity(Term.A(ErlangModuleNames.Module), Term.A(reference.Name), reference.Arity)
        );
    }
}
