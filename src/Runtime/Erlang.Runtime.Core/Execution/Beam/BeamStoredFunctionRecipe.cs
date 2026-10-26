namespace Erlang;

internal sealed record BeamStoredFunctionRecipe(BeamClosureBinding Binding) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
        => BeamClosureFactory.Create(
            Binding.Code,
            Binding.TableIndex,
            environment,
            Binding.Execution,
            legacyIdentity: Binding.Identity
        );
}
