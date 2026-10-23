namespace Erlang;

internal sealed partial class ErlDriverHostEngine420
{
    private long childAttempt455;

    private Task WaitConstruction455(ErlDriverPortIdentity420 identity, ErlDriverPortTaskKind420 kind)
    {
        if (kind == ErlDriverPortTaskKind420.Stop) return Task.CompletedTask;
        lock (gate)
        {
            // A retained async/select job may legitimately outlive the ordinary
            // row. Existing jobs.Invoke still owns delivery versus Drop/free.
            if (!ports.TryGetValue(identity.Port, out var port)) return Task.CompletedTask;
            if (port.Identity.Driver != identity.Driver) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

            return port.Construction455;
        }
    }

    private ErlDriverHostFrame420 ChildRequest455(ErlDriverHostOperation420 operation, ErlDriverPortIdentity420 creator, object value)
        => peer.RequestAsync(
            operation,
            creator.Driver,
            creator.Port,
            0,
            0,
            null,
            ErlDriverPrimitiveWire420.Encode(value),
            CancellationToken.None
        ).AsTask().GetAwaiter().GetResult();

    private nint CreateChild455(
        nint creatorHandle,
        ulong actor,
        byte[] name,
        nint callerOwnedData
    )
    {
        ErlDriverPort420 creator;
        lock (gate)
        {
            if (closing || !ports.TryGetValue(unchecked((ulong)creatorHandle), out creator!) || creator.Closing) return new nint(-1);
        }
        ulong attempt = checked((ulong)Interlocked.Increment(ref childAttempt455));
        var response = ChildRequest455(ErlDriverHostOperation420.ReserveChild455, creator.Identity, new ErlDriverChildReserve455(attempt, actor, name));
        if (response.NativeResult == -1) return new nint(-1);
        if (response.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var reservation = ErlDriverPrimitiveWire420.Decode<ErlDriverChildReservation455>(response.Data);
        if (reservation.Attempt != attempt || reservation.Owner != actor || reservation.Port == 0 || reservation.Port > (ulong)nint.MaxValue
            || ExternalTermFormat.Decode(reservation.PortIdentityEtf) is not PortTerm identity
            || ExternalTermFormat.Decode(reservation.OwnerIdentityEtf) is not Pid owner || identity.Id != reservation.Port)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        ErlDriverPort420? child = null;
        ErlDriverPortRetirement420? preparedRetirement = null;
        bool registeredHold = false;
        nint result;
        try
        {
            nuint portTerm = tokens.Port(reservation.Port, identity);
            nuint connected = tokens.Actor(actor, owner);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(closing, this);
                if (!admittedPortIds455.Add(reservation.Port)) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                ports.EnsureCapacity(checked(ports.Count + 1));
                termPorts.EnsureCapacity(checked(termPorts.Count + 1));
                retirement.EnsureCapacity(checked(retirement.Count + 1));
                child = new(
                    checked((nint)reservation.Port),
                    new(creator.Identity.Driver, reservation.Port),
                    0,
                    false,
                    creator.Slot455,
                    memory!,
                    layout,
                    start!,
                    portTerm,
                    connected,
                    _ => preparedRetirement?.Stop(),
                    select!.MissingReady,
                    constructing455: true
                );
                preparedRetirement = new(child, RetirePort);
                retirement.Add(preparedRetirement.Completion);
                ports.Add(reservation.Port, child);
                termPorts.Add(portTerm, child);
            }
            ErlDriverPortTurn420.RegisterChild455(creatorHandle, child);
            registeredHold = true;
            result = child.NativePort;
            var committed = ChildRequest455(
                ErlDriverHostOperation420.CommitChild455,
                creator.Identity,
                new ErlDriverChildTransition455(attempt, reservation.Port, true)
            );
            if (committed.NativeResult == -1)
            {
                AbortChild455(
                    creator,
                    reservation,
                    child,
                    preparedRetirement is not null,
                    registeredHold
                );

                return new nint(-1);
            }
            if (committed.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        }
        catch (Exception failure)
        {
            try
            {
                AbortChild455(
                    creator,
                    reservation,
                    child,
                    preparedRetirement is not null,
                    registeredHold
                );
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(failure, cleanup);
            }
            throw;
        }
        // No allocation, IPC, callback or disposal may fail after this transfer.
        child.AdoptData455(callerOwnedData);

        return result;
    }

    private void AbortChild455(
        ErlDriverPort420 creator,
        ErlDriverChildReservation455 reservation,
        ErlDriverPort420? child,
        bool prepared,
        bool registeredHold
    )
    {
        List<Exception>? failures = null;
        try
        {
            var reply = ChildRequest455(
                ErlDriverHostOperation420.AbortChild455,
                creator.Identity,
                new ErlDriverChildTransition455(reservation.Attempt, reservation.Port, prepared)
            );
            if (reply.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        }
        catch (Exception failure)
        {
            (failures ??= new()).Add(failure);
        }
        try
        {
            child?.RequestStop();
            if (!registeredHold) child?.ReleaseConstruction455();
        }
        catch (Exception failure)
        {
            (failures ??= new()).Add(failure);
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
