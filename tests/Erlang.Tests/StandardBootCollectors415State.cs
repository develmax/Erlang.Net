namespace Erlang.Tests;

internal sealed record StandardBootCollectors415State(
    string Module,
    ProcessContext Actor,
    object Slot,
    PreparedBeamImage Source,
    ModuleCodeImage Image
);
