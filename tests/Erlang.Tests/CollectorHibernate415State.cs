namespace Erlang.Tests;

internal sealed record CollectorHibernate415State(
    string Module,
    object Slot,
    ProcessContext Context,
    PreparedBeamImage Source,
    ModuleCodeImage Image
);
