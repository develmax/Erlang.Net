namespace Erlang;

internal sealed record LocalFunctionPublication(
    LocalFunctionIdentity Identity,
    int? EnvironmentCount,
    Func<IReadOnlyList<Term>, LocalFunctionInvocation?, FunctionTerm>? Factory,
    bool IsSourceFactory
);
