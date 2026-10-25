namespace Erlang.Compiler;

public sealed partial record ModuleDefinition
{
    IReadOnlyList<(LocalFunctionIdentity Identity, int Arity)> ISourceLocalFunctionResolver.LocalFunctionIdentities()
        => LocalFunctionEntries.Identities(this);

    LocalFunctionIdentity? ISourceLocalFunctionResolver.ResolveLocalIdentity(LocalFunctionIdentity identity, int arity)
        => LocalFunctionEntries.Resolve(this, identity, arity)?.Identity;

    FunctionTerm? ISourceLocalFunctionResolver.ResolveLocalFunction(
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        ModuleCodeImage image,
        LocalFunctionInvocation invocation
    )
    {
        var entry = LocalFunctionEntries.Resolve(this, identity, arity);
        if (entry is null)
            return null;
        var resolved = entry.Value;
        if (resolved.Named is { } name)
        {
            if (environment.Count != 0)
                return null;

            return new FunctionTerm(
                resolved.Identity,
                arity,
                environment,
                (context, arguments) => SourceFunctionInvocation.Named(
                    this,
                    resolved.Identity,
                    name,
                    (ProcessContext)context,
                    image,
                    arguments,
                    invocation.OriginalFunction,
                    invocation
                )
            );
        }
        Expr.Fun expression = resolved.Anonymous!;
        string[]? names = ClosureCaptures.Names(expression);
        if (names is null)
        {
            Semantics.Validate(this);
            names = ClosureCaptures.Names(expression);
        }
        if (names is null || names.Length != environment.Count)
            return null;
        var captured = names.Select((captureName, index) => new KeyValuePair<string, Term>(captureName, environment[index]))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        return new FunctionTerm(
            resolved.Identity,
            arity,
            environment,
            (context, arguments) => SourceFunctionInvocation.Anonymous(
                this,
                resolved.Identity,
                expression,
                captured,
                (ProcessContext)context,
                image,
                arguments,
                invocation.OriginalFunction,
                invocation
            )
        );
    }
}
