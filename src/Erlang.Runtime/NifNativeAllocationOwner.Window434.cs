namespace Erlang;

internal sealed unsafe partial class NifNativeAllocationOwner
{
    internal void RegisterWindow434(string module)
    {
        lock (gate)
        {
            if (domain is null || domainEnded) throw new InvalidOperationException(AllocatorGatherNames434.Membership);
            domain.RegisterScanOwner434(domainMembership, this, module);
        }
    }

    internal AllocatorBlockWindow434[] CaptureWindow434(long membership, string module)
    {
        lock (gate)
        {
            if (domainEnded || domainMembership != membership) return [];

            // This actual pointer->size table is also the source of real
            // reallocate/free ownership validation. No shadow Term graph.
            return owned.Values.Select(size => new AllocatorBlockWindow434(
                AllocatorBackendNames.Driver,
                AllocatorGatherNames434.NifType,
                Term.A(module),
                BackingRequest(size)
            )).ToArray();
        }
    }
}
