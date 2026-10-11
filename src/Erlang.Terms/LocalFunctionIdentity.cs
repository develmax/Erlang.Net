namespace Erlang;

public sealed record LocalFunctionIdentity(
    Atom Module,
    Atom Name,
    int Index,
    int OldUnique,
    BitString Unique
);
