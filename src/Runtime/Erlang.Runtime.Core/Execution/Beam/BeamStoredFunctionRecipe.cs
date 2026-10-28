namespace Erlang;

internal sealed record BeamStoredFunctionRecipe(BeamClosureBinding Binding) : IRuntimeStoredFunctionRecipe, ICanonicalGuestRecipe507
{
    bool ICanonicalGuestRecipe507.AdmittedTo507(ProcessRuntime runtime)
        => Binding.Execution.Owner is PreparedBeamImage image && ReferenceEquals(image.Owner, runtime.Modules)
            && image.ExecutionIdentity.NativePrimitives.Values.All(body => runtime.Modules.NativeBorrowKind507(image.ExecutionIdentity, body) != OwnedBindingKind507.Opaque);

    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
        => BeamClosureFactory.Create(
            Binding.Code,
            Binding.TableIndex,
            environment,
            Binding.Execution,
            legacyIdentity: Binding.Identity
        );
}
