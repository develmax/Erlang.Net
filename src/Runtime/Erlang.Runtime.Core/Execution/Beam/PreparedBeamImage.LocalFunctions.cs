namespace Erlang;

public sealed partial class PreparedBeamImage
{
    IReadOnlyList<(LocalFunctionIdentity Identity, int Arity)> ISourceLocalFunctionResolver.LocalFunctionIdentities()
        => Code.Lambdas.Select((lambda, index) => (
            new LocalFunctionIdentity(
                Term.A(Name),
                Term.A(Code.Structure.Atoms[lambda.FunctionAtomIndex]!),
                lambda.StoredIndex,
                lambda.OldUniqueness,
                Descriptor.Fingerprint,
                index
            ),
            lambda.TotalArity - lambda.FreeVariables
        )).ToArray();

    private int LocalFunctionTableIndex(LocalFunctionIdentity identity, int arity)
    {
        if (identity.Module.Name != Name || !identity.Unique.Equals(Descriptor.Fingerprint))
            return -1;
        for (int index = 0; index < Code.Lambdas.Count; index++)
        {
            BeamLambdaEntry lambda = Code.Lambdas[index];
            if (lambda.StoredIndex == identity.Index && lambda.TotalArity - lambda.FreeVariables == arity)
                return index;
        }

        return -1;
    }

    LocalFunctionIdentity? ISourceLocalFunctionResolver.ResolveLocalIdentity(LocalFunctionIdentity identity, int arity)
    {
        int index = LocalFunctionTableIndex(identity, arity);

        if (index < 0) return null;
        BeamLambdaEntry lambda = Code.Lambdas[index];

        return new LocalFunctionIdentity(
            Term.A(Name),
            Term.A(Code.Structure.Atoms[lambda.FunctionAtomIndex]!),
            lambda.StoredIndex,
            lambda.OldUniqueness,
            Descriptor.Fingerprint,
            index
        );
    }

    FunctionTerm? ISourceLocalFunctionResolver.ResolveLocalFunction(
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        ModuleCodeImage image,
        LocalFunctionInvocation invocation
    )
    {
        int index = LocalFunctionTableIndex(identity, arity);
        if (index < 0 || environment.Count != Code.Lambdas[index].FreeVariables
            || !ReferenceEquals(image, ExecutionIdentity.Image))
            return null;

        return BeamClosureFactory.Create(
            Code,
            index,
            environment,
            ExecutionIdentity,
            invocation,
            legacyIdentity: invocation.Publication.Identity
        );
    }
}
