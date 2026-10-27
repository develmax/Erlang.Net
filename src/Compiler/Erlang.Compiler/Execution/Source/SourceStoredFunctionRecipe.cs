namespace Erlang.Compiler;

internal sealed class SourceStoredFunctionRecipe : IRuntimeStoredFunctionRecipe, ICanonicalGuestRecipe507
{
    private readonly RuntimeFunctionEntries owner;
    private readonly ModuleDefinition? module;
    private readonly LocalFunctionIdentity identity;
    private readonly Expr.Fun? expression;
    private readonly string? name;
    private readonly string[] names;
    private readonly ModuleCodeImage? image;
    private readonly int arity;

    private SourceStoredFunctionRecipe(
        RuntimeFunctionEntries owner,
        ModuleDefinition? module,
        LocalFunctionIdentity identity,
        Expr.Fun? expression,
        string? name,
        IEnumerable<string> names,
        ModuleCodeImage? image,
        int arity
    )
    {
        this.owner = owner;
        this.module = module;
        this.identity = identity;
        this.expression = expression;
        this.name = name;
        this.names = names.ToArray();
        this.image = image;
        this.arity = arity;
    }

    internal static SourceStoredFunctionRecipe Register(
        RuntimeFunctionEntries owner,
        ModuleDefinition? module,
        LocalFunctionIdentity identity,
        Expr.Fun? expression,
        string? name,
        IEnumerable<string> names,
        ModuleCodeImage? image,
        int arity
    )
    {
        // Publication preserves the first entry's legacy hash fields. Resolve
        // those before publishing an immutable typed factory receiver.
        LocalFunctionIdentity previous = owner.GetCanonicalIdentityForHash(identity, arity);
        identity = identity with { OldUnique = previous.OldUnique, LegacyIndex = previous.OldIndex };
        var recipe = new SourceStoredFunctionRecipe(
            owner,
            module,
            identity,
            expression,
            name,
            names,
            image,
            arity
        );
        _ = owner.RegisterSourceFactory(
            identity,
            arity,
            recipe.names.Length,
            recipe.Create
        );

        return recipe;
    }

    bool ICanonicalGuestRecipe507.AdmittedTo507(ProcessRuntime runtime) => ReferenceEquals(owner, runtime.Functions);

    public FunctionTerm Rebind(IReadOnlyList<Term> environment) => Create(environment);

    internal FunctionTerm Create(IReadOnlyList<Term> environment, LocalFunctionInvocation? invocation = null)
    {
        if (environment.Count != names.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        IReadOnlyList<Term> captured = Array.AsReadOnly(environment.ToArray());
        var bindings = names.Select((name, index) => new KeyValuePair<string, Term>(name, captured[index]))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        FunctionTerm? result = null;
        result = new FunctionTerm(
            identity,
            arity,
            captured,
            (context, arguments) => name is not null
                ? SourceFunctionInvocation.Named(
                    module!,
                    identity,
                    name,
                    (ProcessContext)context,
                    image,
                    arguments,
                    invocation?.OriginalFunction ?? result!,
                    invocation
                )
                : SourceFunctionInvocation.Anonymous(
                    module,
                    identity,
                    expression!,
                    bindings,
                    (ProcessContext)context,
                    image,
                    arguments,
                    invocation?.OriginalFunction ?? result!,
                    invocation
                )
        );
        SourceFunctionTailDispatch.Register(result);
        owner.RegisterStoredSource(result, this);

        return result;
    }
}
