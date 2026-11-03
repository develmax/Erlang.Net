namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal Task KillDriver(DdllParentRow422 driver, DriverParentPeerTransport422 transport)
    {
        NativeDriverPortRow422[] rows;
        lock (gate)
        {
            rows = native.Values.Where(row => row.Native.Driver == driver.Identity && !row.LogicalClosePublished1311).ToArray();
            foreach (var row in rows)
            {
                row.StopRequested = true;
                row.SetReason641(Term.A(NativeDriverPortNames422.DriverUnloaded));
                row.Writable.TrySetResult();
            }
        }

        return CloseRows(rows, transport, waitPhysical: false);
    }

    internal Task CloseAll(DriverParentPeerTransport422 transport)
    {
        NativeDriverPortRow422[] rows;
        lock (gate)
        {
            rows = native.Values.ToArray();
            foreach (var row in rows)
            {
                row.StopRequested = true;
                row.SetReason641(Term.A(ProcessExitReasons.Shutdown));
                row.Writable.TrySetResult();
            }
        }

        return CloseRows(rows, transport);
    }

    private async Task CloseRows(
        NativeDriverPortRow422[] rows,
        DriverParentPeerTransport422 transport,
        bool waitPhysical = true
    )
    {
        // Stage every stop before waiting one final ACK. Stop callbacks and
        // async/select/process_exit cleanup remain on the real retained queue.
        var closes = new List<Task>(rows.Length);
        var failures = new List<Exception>();
        foreach (var row in rows)
        {
            try
            {
                closes.Add(Close(row, transport));
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        foreach (var task in closes)
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        foreach (var row in rows)
            try
            {
                // Logical port death is not proof of late async/select cleanup.
                if (waitPhysical) await row.Stopped.Task.ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        foreach (var row in rows)
            try
            {
                await row.Commands.ConfigureAwait(false);
            }
            catch (ErlangException) { /* Source badarg is already delivered to its caller. */ }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    internal void ChildPhysicallySettled(Exception failure)
    {
        lock (gate)
        {
            foreach (var row in native.Values.ToArray())
            {
                // Only the session's post-WaitForExit path may use this fence.
                // It is failure reclamation, never a successful native stop ACK.
                row.Closing = true;
                row.PhysicallyStopped = true;
                row.Writable.TrySetResult();
                row.Closed1311.TrySetException(failure);
                row.Stopped.TrySetException(failure);
                row.Links.Clear();
                row.Lines = null;
                row.Data = Nil.Value;
                row.ReleaseReason641();
            }
            childAttempts455.Clear();
            ownPortIdentities455.Clear();
            native.Clear();
            ports.Clear();
        }
    }
}
