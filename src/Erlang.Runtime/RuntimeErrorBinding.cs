namespace Erlang;

internal sealed record RuntimeErrorBinding(
    ErlangFunction Body,
    ModuleCodeImage Image,
    bool Native
)
{
    internal RuntimeNativePrimitives Provider { get; init; }
}
