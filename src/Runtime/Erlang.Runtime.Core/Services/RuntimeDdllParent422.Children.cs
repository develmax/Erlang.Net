namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    internal bool ChildPortAllowed455(ulong driver)
        => !closing && identities.TryGetValue(driver, out var row)
            && row.State is not (DdllParentStateKind422.ForceUnloading or DdllParentStateKind422.ForceReloading);

    internal DdllParentRow422? AcquireChildPort455(ErlDriverPortIdentity420 creator, ulong child)
    {
        // Nested driver_create_port retains the EXACT entered driver. Ordinary
        // pending mutations do not block this path: driver_ok rejects force
        // states only, and waiting here could wait on our own native callback.
        if (!ChildPortAllowed455(creator.Driver)) return null;
        var row = identities[creator.Driver];
        if (!row.Ports.Contains(creator.Port)) throw new InvalidDataException(DriverParentWireNames422.Invalid);
        if (!row.Ports.Add(child)) throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return row;
    }
}
