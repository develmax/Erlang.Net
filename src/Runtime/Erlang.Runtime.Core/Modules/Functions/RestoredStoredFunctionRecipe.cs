namespace Erlang;

internal sealed record RestoredStoredFunctionRecipe(
    RuntimeFunctionEntries Owner,
    ProcessRuntime Runtime,
    LocalFunctionIdentity Identity,
    int Arity
) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
        => Owner.CreateStoredRestored(
            Runtime,
            Identity,
            Arity,
            environment
        );
}
