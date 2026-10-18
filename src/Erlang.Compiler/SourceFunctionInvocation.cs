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
        FunctionTerm functionTerm,
        LocalFunctionInvocation? invocation = null
    )
    {
        var admission = await context.Runtime.Modules.AdmitSourceFunction(
            context,
            module.Name,
            identity.Unique,
            module,
            image,
            functionTerm,
            invocation
        );
        if (admission.Kind == SourceFunctionAdmissionKind.Retry)
            throw new LocalFunctionPublicationChangedException(invocation!);
        if (admission.Kind == SourceFunctionAdmissionKind.MissingCurrentLocalFunction)
        {
            SourceFunctionTailDispatch.Cancel(context);

            return await context.Runtime.Modules.InvokeUndefinedLambda(
                context,
                module.Name,
                functionTerm,
                arguments,
                invocation
            );
        }
        module = (ModuleDefinition)admission.Owner;
        image = admission.Image;

        SourceFunctionTailDispatch.Transfer(context, new(
            module,
            function,
            null,
            null,
            null,
            image,
            admission.Lease
        ));

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
        FunctionTerm functionTerm,
        LocalFunctionInvocation? invocation = null
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
                functionTerm,
                invocation
            );
            if (admission.Kind == SourceFunctionAdmissionKind.Retry)
                throw new LocalFunctionPublicationChangedException(invocation!);
            if (admission.Kind == SourceFunctionAdmissionKind.MissingCurrentLocalFunction)
            {
                SourceFunctionTailDispatch.Cancel(context);

                return await context.Runtime.Modules.InvokeUndefinedLambda(
                    context,
                    module.Name,
                    functionTerm,
                    arguments,
                    invocation
                );
            }
            if (!ReferenceEquals(module, admission.Owner))
                expression = LocalFunctionEntries.Anonymous((ModuleDefinition)admission.Owner, identity.Index);
            module = (ModuleDefinition)admission.Owner;
            image = admission.Image;
            lease = admission.Lease;
        }
        else if (invocation is not null && !context.Runtime.Modules.ValidateLocalFunctionPublication(invocation))
            throw new LocalFunctionPublicationChangedException(invocation);

        SourceFunctionTailDispatch.Transfer(context, new(
            module,
            null,
            identity,
            expression,
            environment,
            image,
            lease
        ));

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
