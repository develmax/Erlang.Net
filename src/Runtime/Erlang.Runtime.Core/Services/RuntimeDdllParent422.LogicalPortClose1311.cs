namespace Erlang;

internal sealed partial class RuntimeDdllParent422
{
    internal void PortClosed1311(ulong driver, ulong port)
    {
        lock (gate)
        {
            if (!identities.TryGetValue(driver, out var row) || !row.Ports.Contains(port)
                || row.ClosedPorts1311.Contains(port))
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            // Capacity was reserved at admission before native start. Keep the
            // physical Ports entry until late work/PDL/image cleanup is ACKed.
            if (!row.ClosedPorts1311.Add(port))
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
        }
    }
}
