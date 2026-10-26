namespace Erlang;

internal sealed partial class DriverParentPeerTransport422
{
    internal async ValueTask DistributionOutput457(ErlDriverPortIdentity420 identity, byte[] packet)
    {
        var reply = await Request(
            ErlDriverHostOperation420.DistributionOutput457,
            identity.Driver,
            identity.Port,
            0,
            0,
            null,
            packet,
            CancellationToken.None,
            true
        ).ConfigureAwait(false);
        if (reply.NativeResult != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }
}
