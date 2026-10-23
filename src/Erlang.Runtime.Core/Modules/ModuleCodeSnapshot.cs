namespace Erlang;

internal sealed record ModuleCodeSnapshot(
    IReadOnlyList<((string Module, string Function, int Arity) Signature, ErlangFunction Body)> Entries,
    ModuleCodeDescriptor? Descriptor,
    ModuleCodeImage Image,
    IReadOnlyList<object> SourceOwners
);
