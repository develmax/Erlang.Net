namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal Task OwnerTerminated(
        Pid owner,
        Term reason,
        DriverParentPeerTransport422 transport,
        RuntimeProcessPayload641? reason641 = null
    )
    {
        var stopping = new List<NativeDriverPortRow422>();
        lock (gate)
        {
            foreach (var row in native.Values)
            {
                if (!row.Links.Remove(owner)) continue;
                if (row.Connected.Equals(owner) || !reason.Equals(Term.A(ProcessExitReasons.Normal)))
                {
                    row.StopRequested = true;
                    row.SetReason641(reason, reason641);
                    row.Writable.TrySetResult();
                    stopping.Add(row);
                }
            }
        }

        return CloseRows(stopping.ToArray(), transport);
    }
}
