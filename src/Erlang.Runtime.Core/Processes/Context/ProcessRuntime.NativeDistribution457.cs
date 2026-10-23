namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal NativeDistributionBinding457 BindNativeDistribution457(PortTerm port, RuntimeDistributionPendingEntry entry)
    {
        lock (gate)
        {
            if (port.LocalIdentity is not null && !ReferenceEquals(port.LocalIdentity, LocalIdentity))
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return (driverFrontend422 ?? throw new ErlangException(ErlangErrorReasons.BadArgument)).BindDistribution457(port, entry);
        }
    }
}
