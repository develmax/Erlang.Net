namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal void Closed1311(ErlDriverPortIdentity420 identity, ErlDriverClosed1311 stopped)
    {
        lock (gate)
        {
            var row = Find(identity) ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
            if (row.LogicalStopReceived1311 || row.PhysicallyStopped || !stopped.NativeStopped)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            row.LogicalStopReceived1311 = true;
            row.Closing = true;
            // A failed start or uncommitted child still requires the original
            // physical cleanup fence before releasing its DDLL reservation.
            if (row.OpenResultKnown && row.OpenSucceeded)
            {
                try
                {
                    FinishLogicalStopped1311(row);
                }
                catch (Exception failure)
                {
                    row.Closed1311.TrySetException(failure);
                    throw;
                }
            }
        }
    }

    private void FinishLogicalStopped1311(NativeDriverPortRow422 row)
    {
        if (row.LogicalClosePublished1311) return;
        row.LogicalClosePublished1311 = true;
        ports.Remove(row.Identity);
        row.Writable.TrySetResult();
        var failures = row.LogicalCloseFailures1311;
        try
        {
            if (row.OpenSucceeded && !row.NativeReferenceReleased)
            {
                drivers.PortClosed1311(row.Native.Driver, row.Native.Port);
            }
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            row.Distribution457?.PortStopped(row.Reason);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            if (row.OpenSucceeded) runtime.PortRemoved(
                row.Identity,
                row.Links.ToArray(),
                row.Reason,
                row.Reason641
            );
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        row.Links.Clear();
        row.Data = Nil.Value;
        try
        {
            row.ReleaseReason641();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        row.Lines = null;
        row.Command = [];
        if (failures.Count == 0) row.Closed1311.TrySetResult();
        else row.Closed1311.TrySetException(failures);
    }
}
