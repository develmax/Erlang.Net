namespace Erlang.Compiler;

internal static class SourceFunctionInvocation
{
    public static ValueTask<Term> Named(
        ModuleDefinition module,
        LocalFunctionIdentity identity,
        string function,
        ProcessContext context,
        ModuleCodeImage? image,
        IReadOnlyList<Term> arguments
    )
    {
        if (context.Runtime.Modules.SourceForCode(module.Name, identity.Unique) is { Owner: ModuleDefinition current } entry)
        {
            module = current;
            image = entry.Image;
        }

        return Execution.InvokeAsync(
            module,
            function,
            context,
            arguments,
            image
        );
    }

    public static ValueTask<Term> Anonymous(
        ModuleDefinition? module,
        LocalFunctionIdentity identity,
        Expr.Fun expression,
        Dictionary<string, Term> environment,
        ProcessContext context,
        ModuleCodeImage? image,
        IReadOnlyList<Term> arguments
    )
    {
        if (module is not null && context.Runtime.Modules.SourceForCode(module.Name, identity.Unique) is { Owner: ModuleDefinition current } entry)
        {
            module = current;
            image = entry.Image;
            expression = LocalFunctionEntries.Anonymous(current, identity.Index);
        }

        return Execution.InvokeAnonymousAsync(
            module,
            identity,
            expression,
            environment,
            context,
            image,
            arguments
        );
    }
}
