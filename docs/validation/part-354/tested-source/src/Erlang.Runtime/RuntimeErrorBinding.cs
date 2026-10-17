namespace Erlang;

internal sealed record RuntimeErrorBinding(
    ErlangFunction Body,
    ModuleCodeImage Image,
    bool Native
);
