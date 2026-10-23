namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
    internal NativeDistributionBinding457 BindDistribution457(PortTerm port, RuntimeDistributionPendingEntry entry)
    {
        RequireReady();

        return ports.BindDistribution457(port, entry, transport);
    }
}
