namespace Erlang;

internal interface ISourceLocalFunctionResolver
{
    IReadOnlyList<(LocalFunctionIdentity Identity, int Arity)> LocalFunctionIdentities();

    LocalFunctionIdentity? ResolveLocalIdentity(LocalFunctionIdentity identity, int arity);

    FunctionTerm? ResolveLocalFunction(
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        ModuleCodeImage image,
        LocalFunctionInvocation invocation
    );
}
