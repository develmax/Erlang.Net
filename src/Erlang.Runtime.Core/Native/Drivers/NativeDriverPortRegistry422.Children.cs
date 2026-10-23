namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    private readonly Dictionary<(ErlDriverPortIdentity420 Creator, ulong Attempt), ErlDriverChildAttempt455> childAttempts455 = new();

    internal ErlDriverChildReservation455? ReserveChild455(ErlDriverPortIdentity420 creator, ErlDriverChildReserve455 request, Pid owner)
    {
        lock (gate)
        {
            if (request.Attempt == 0 || request.Owner == 0 || request.Name.Length == 0 || request.Name.Length > ErlDriverChildNames455.MaximumNameBytes)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            var key = (creator, request.Attempt);
            if (childAttempts455.TryGetValue(key, out var prior))
            {
                if (prior.Request.Owner != request.Owner || !prior.Request.Name.AsSpan().SequenceEqual(request.Name))
                    throw new InvalidDataException(DriverParentWireNames422.Invalid);

                return prior.Aborted ? null : prior.Reservation;
            }
            var parent = Find(creator);
            if (parent is null || parent.Closing || parent.StopRequested || parent.PhysicallyStopped
                || !runtime.DriverTargetInternal422(owner) || !runtime.DriverTargetAlive422(owner)) return null;
            PortTerm identity = runtime.Ports.ReserveDriverIdentity422();
            var driver = drivers.AcquireChildPort455(creator, identity.Id);
            if (driver is null) return null;
            NativeDriverPortRow422? row = null;
            bool linked = false, remembered = false;
            try
            {
                row = new(
                    new(creator.Driver, identity.Id),
                    identity,
                    owner,
                    PortOpenOptions.Child455(request.Name, parent.Options.Parallelism)
                )
                {
                    DriverSoftBusy = (driver.NativeFlags & NativeDriverPortNames422.SoftBusyFlag) != 0,
                    Command = request.Name.ToArray()
                };
                var reservation = new ErlDriverChildReservation455(
                    request.Attempt,
                    identity.Id,
                    request.Owner,
                    ExternalTermFormat.Encode(identity),
                    ExternalTermFormat.Encode(owner)
                );
                var transaction = new ErlDriverChildAttempt455(request, reservation, row);
                row.ChildAttempt455 = transaction;
                linked = runtime.AddPortLink(owner, identity);
                if (!linked) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
                native.Add(row.Native, row);
                ports.Add(identity, row);
                ownPortIdentities455.Add(identity.Id, identity);
                remembered = true;
                childAttempts455.Add(key, transaction);

                return reservation;
            }
            catch
            {
                if (row is not null)
                {
                    native.Remove(row.Native);
                    ports.Remove(identity);
                }
                if (remembered) ownPortIdentities455.Remove(identity.Id);
                if (linked) runtime.RemoveDriverConnectedLink422(owner, identity);
                drivers.PortStopped(creator.Driver, identity.Id);
                throw;
            }
        }
    }

    internal bool CommitChild455(ErlDriverPortIdentity420 creator, ErlDriverChildTransition455 request)
    {
        lock (gate)
        {
            var attempt = ChildAttempt455(creator, request);
            if (attempt.Aborted) return false;
            if (attempt.Committed) return true;
            var row = attempt.Row ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
            if (!request.Prepared || row.Closing || row.StopRequested || !runtime.DriverTargetAlive422(row.Connected)
                || !drivers.ChildPortAllowed455(creator.Driver)) return false;
            attempt.Committed = true;
            row.OpenResultKnown = true;
            row.OpenSucceeded = true;

            return true;
        }
    }

    internal void AbortChild455(ErlDriverPortIdentity420 creator, ErlDriverChildTransition455 request)
    {
        lock (gate)
        {
            var attempt = ChildAttempt455(creator, request);
            if (attempt.Aborted) return;
            attempt.Aborted = true;
            var row = attempt.Row ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
            runtime.RemoveDriverConnectedLink422(row.Connected, row.Identity);
            row.Links.Clear();
            row.Closing = true;
            row.OpenResultKnown = true;
            row.OpenSucceeded = false;
            row.Writable.TrySetResult();
            if (request.Prepared)
            {
                // A local row/image lease exists; only its real retirement ACK
                // may release the DDLL reference, even though data is unowned.
                if (row.PhysicallyStopped) FinishStopped(row);

                return;
            }
            // Authenticated engine certifies that no local port was prepared.
            // This is reservation rollback, not a fabricated PortStopped ACK.
            native.Remove(row.Native);
            ports.Remove(row.Identity);
            drivers.PortStopped(creator.Driver, row.Native.Port);
            row.NativeReferenceReleased = true;
            attempt.Row = null;
            row.Stopped.TrySetResult();
        }
    }

    private ErlDriverChildAttempt455 ChildAttempt455(ErlDriverPortIdentity420 creator, ErlDriverChildTransition455 request)
    {
        if (!childAttempts455.TryGetValue((creator, request.Attempt), out var attempt) || attempt.Reservation.Port != request.Port)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return attempt;
    }
}
