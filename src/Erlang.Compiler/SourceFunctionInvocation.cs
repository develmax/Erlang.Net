namespace Erlang.Compiler;

internal static class SourceFunctionInvocation
{
    public static async ValueTask<Term> Named(
        ModuleDefinition module,
        LocalFunctionIdentity identity,
        string function,
        ProcessContext context,
        ModuleCodeImage? image,
        IReadOnlyList<Term> arguments,
        FunctionTerm functionTerm
    )
    {
        var admission = await context.Runtime.Modules.AdmitSourceFunction(
            context,
            module.Name,
            identity.Unique,
            module,
            image,
            functionTerm
        );
        module = (ModuleDefinition)admission.Owner;
        image = admission.Image;

        return await Execution.InvokeAsync(
            module,
            function,
            context,
            arguments,
            image,
            admission.Lease
        );
    }

    public static async ValueTask<Term> Anonymous(
        ModuleDefinition? module,
        LocalFunctionIdentity identity,
        Expr.Fun expression,
        Dictionary<string, Term> environment,
        ProcessContext context,
        ModuleCodeImage? image,
        IReadOnlyList<Term> arguments,
        FunctionTerm functionTerm
    )
    {
        SourceCodeLease? lease = null;
        if (module is not null)
        {
            var admission = await context.Runtime.Modules.AdmitSourceFunction(
                context,
                module.Name,
                identity.Unique,
                module,
                image,
                functionTerm
            );
            if (!ReferenceEquals(module, admission.Owner))
                expression = LocalFunctionEntries.Anonymous((ModuleDefinition)admission.Owner, identity.Index);
            module = (ModuleDefinition)admission.Owner;
            image = admission.Image;
            lease = admission.Lease;
        }

        return await Execution.InvokeAnonymousAsync(
            module,
            identity,
            expression,
            environment,
            context,
            image,
            arguments,
            lease
        );
    }
}
