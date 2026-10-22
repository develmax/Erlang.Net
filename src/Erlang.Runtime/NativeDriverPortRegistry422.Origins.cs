namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal void RequireNativeOrigin(ErlDriverPortIdentity420 identity)
    {
        lock (gate)
            if (Find(identity) is not { PhysicallyStopped: false })
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }

    internal PortTerm OriginIdentity(ErlDriverPortIdentity420 identity)
    {
        lock (gate)
        {
            RequireNativeOrigin(identity);

            return Find(identity)!.Identity;
        }
    }
}
