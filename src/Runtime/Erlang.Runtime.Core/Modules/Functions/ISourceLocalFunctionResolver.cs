namespace Erlang;

internal interface ISourceLocalFunctionResolver
{
    IReadOnlyList<(LocalFunctionIdentity Identity, int Arity)> LocalFunctionIdentities();

    LocalFunctionIdentity? ResolveLocalIdentity(LocalFunctionIdentity identity, int arity);

    // Called only after actual receiver module/image provenance is authenticated.
    FunctionTerm? ReconstructPublicLocal414(
        ProcessRuntime runtime,
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        ModuleCodeImage image
    );

    FunctionTerm? ResolveLocalFunction(
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        ModuleCodeImage image,
        LocalFunctionInvocation invocation
    );
}
