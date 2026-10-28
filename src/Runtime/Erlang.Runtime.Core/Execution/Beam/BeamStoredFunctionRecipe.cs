namespace Erlang;

internal sealed record BeamStoredFunctionRecipe(BeamClosureBinding Binding) : IRuntimeStoredFunctionRecipe, ICanonicalGuestRecipe507
{
    bool ICanonicalGuestRecipe507.AdmittedTo507(ProcessRuntime runtime)
        // The recipe names admitted BEAM code; native entries still pass through
        // the interpreter's per-image/per-delegate opaque or reviewed boundary.
        => Binding.Execution.Owner is PreparedBeamImage image && ReferenceEquals(image.Owner, runtime.Modules);

    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
        => BeamClosureFactory.Create(
            Binding.Code,
            Binding.TableIndex,
            environment,
            Binding.Execution,
            legacyIdentity: Binding.Identity
        );
}
