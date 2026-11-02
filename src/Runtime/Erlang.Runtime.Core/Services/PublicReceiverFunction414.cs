namespace Erlang;

// A per-public-operation resolver, never ambient or accepted by raw heap APIs.
internal sealed class PublicReceiverFunction414(ProcessRuntime runtime)
{
    internal bool TryRebind(
        ProcessRuntime receiver,
        FunctionTerm source,
        IReadOnlyList<Term> environment,
        out FunctionTerm? result
    )
    {
        result = null;
        if (!ReferenceEquals(runtime, receiver) || source.OwnedFunctionStorage is not null
            || !source.HasRuntimeLocalTransferDescriptor) return false;
        LocalFunctionIdentity? identity = source.StoredLocalIdentity ?? source.RuntimeLocalTransferIdentity414;
        if (identity is null) return false;
        result = runtime.Modules.ReconstructPublicLocal414(
            runtime,
            identity,
            source.Arity,
            environment
        );

        return result is not null;
    }
}
