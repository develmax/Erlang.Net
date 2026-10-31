namespace Erlang;

public sealed partial class FunctionTerm
{
    private volatile bool runtimeLocalTransferDescriptor;
    internal bool HasRuntimeLocalTransferDescriptor => runtimeLocalTransferDescriptor;

    // Metadata guard only: never a callable/guest borrowing capability.
    // Existing external pointers retain their VM-independent dispatch contract.
    internal void MarkRuntimeLocalTransferDescriptor()
    {
        if (ExternalIdentity is null)
            runtimeLocalTransferDescriptor = true;
    }

    private FunctionTerm(
        int arity,
        Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> body,
        ExternalFunctionIdentity? external,
        LocalFunctionIdentity? local,
        Func<LocalFunctionIdentity>? provider,
        long identity,
        IReadOnlyList<Term> environment
    ) : this(arity, body)
    {
        ExternalIdentity = external;
        localIdentity = local;
        identityProvider = provider;
        Identity = identity;
        this.environment = Array.AsReadOnly(environment.ToArray());
        // No runtime descriptor/source recipe is installed for this Host copy.
    }
}
