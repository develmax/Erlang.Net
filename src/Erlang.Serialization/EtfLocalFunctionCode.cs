namespace Erlang;

public sealed record EtfLocalFunctionCode(
    Atom Module,
    int Index,
    int OldIndex,
    int OldUnique,
    BitString Unique
);
