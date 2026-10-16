namespace Erlang;

internal interface ISourceLocalFunctionResolver
{
    LocalFunctionIdentity? ResolveLocalIdentity(LocalFunctionIdentity identity, int arity);

    FunctionTerm? ResolveLocalFunction(
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        ModuleCodeImage image,
        LocalFunctionInvocation invocation
    );
}
