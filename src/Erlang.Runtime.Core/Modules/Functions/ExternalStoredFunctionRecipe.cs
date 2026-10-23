namespace Erlang;

internal sealed record ExternalStoredFunctionRecipe(RuntimeFunctionEntries Owner, ExternalFunctionIdentity Identity) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        if (environment.Count != 0) throw new InvalidOperationException(RuntimeEtsFoundationNames.UnsupportedFunction);

        return Owner.ExternalForArtifactInspection(Identity);
    }
}
