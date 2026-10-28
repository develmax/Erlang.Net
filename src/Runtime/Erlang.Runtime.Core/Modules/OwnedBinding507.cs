namespace Erlang;

internal sealed record OwnedBinding507(
    ErlangFunction Body,
    ModuleCodeImage Image,
    object? SourceOwner,
    OwnedBindingKind507 Kind
);
