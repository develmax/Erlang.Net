namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    // Like the host's token book, retain identifiers until physical session
    // retirement. A closed port remains a valid term; no row/lease is retained.
    private readonly Dictionary<ulong, PortTerm> ownPortIdentities455 = new();

    internal PortTerm ResolveOwnPort455(ulong handle)
    {
        lock (gate)
            return ownPortIdentities455.GetValueOrDefault(handle)
                ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }

    internal void ReleaseOwnPortIdentities455()
    {
        lock (gate)
        {
            ownPortIdentities455.Clear();
            childAttempts455.Clear();
        }
    }
}
