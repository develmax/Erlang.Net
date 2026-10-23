namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    // Options/command are already parsed by the actual native-open adapter.
    // Reserve the shared VM port ID and real DDLL port reference before start,
    // because start itself can synchronously emit reverse output/monitor calls.
    internal async ValueTask<Term> Open(
        ProcessContext owner,
        ErlDriverName420 name,
        byte[] command,
        PortOpenOptions options,
        DriverParentPeerTransport422 transport,
        DriverFrontendConfiguration422 configuration
    )
    {
        runtime.RequireDriverParentActor422(owner);
        PortTerm identity = runtime.Ports.ReserveDriverIdentity422();
        DdllParentRow422 driver = await drivers.AcquirePort(owner, name, identity.Id).ConfigureAwait(false);
        NativeDriverPortRow422 row;
        try
        {
            row = new(
                new(driver.Identity, identity.Id),
                identity,
                owner.Self,
                options
            )
            {
                DriverSoftBusy = (driver.NativeFlags & NativeDriverPortNames422.SoftBusyFlag) != 0,
                Command = command
            };
        }
        catch
        {
            drivers.PortStopped(driver.Identity, identity.Id);
            throw;
        }
        lock (gate)
        {
            try
            {
                native.Add(row.Native, row);
                try
                {
                    ports.Add(identity, row);
                    try
                    {
                        ownPortIdentities455.Add(identity.Id, identity);
                    }
                    catch
                    {
                        ports.Remove(identity);
                        throw;
                    }
                }
                catch
                {
                    native.Remove(row.Native);
                    throw;
                }
            }
            catch
            {
                drivers.PortStopped(row.Native.Driver, row.Native.Port);
                throw;
            }
        }
        // Once the child request is admitted, actor cancellation cannot abandon
        // the response or provisional native owner. Caller observes cancellation
        // only after the authoritative start result/failed-start fence is owned.
        ErlDriverOpenResult420 result = await transport.Open(
            row.Native,
            identity,
            owner.Self,
            command,
            (options.Binary ? NativeDriverPortNames422.BinaryFlag : 0)
                | (options.LineBytes != 0 ? NativeDriverPortNames422.LineFlag : 0),
            options.Eof,
            CancellationToken.None
        ).ConfigureAwait(false);
        Task failedStart;
        lock (gate)
        {
            row.OpenResultKnown = true;
            row.OpenSucceeded = result.StartError == 0;
            if (row.OpenSucceeded)
            {
                // Actual process links are installed only after successful start.
                // An actor that died during start does not get resurrected.
                if (runtime.DriverTargetAlive422(owner.Self)) owner.PortLinks.Add(identity);
                else
                {
                    row.Links.Clear();
                    row.Closing = true;
                }
            }
            else
            {
                row.Links.Clear();
                row.Closing = true;
            }
            if (row.PhysicallyStopped) FinishStopped(row);
            failedStart = row.Stopped.Task;
        }
        if (result.StartError != 0)
        {
            // Failed start also has a separate PortStopped ACK; Open reply is
            // not proof that image/service/monitor cleanup has completed.
            await failedStart.ConfigureAwait(false);
            throw new ErlangException(result.StartError switch
            {
                NativeDriverPortNames422.StartGeneral => NativeDriverPortNames422.InvalidValue,
                NativeDriverPortNames422.StartErrno => configuration.Errno(result.NativeErrno),
                NativeDriverPortNames422.StartBadArgument => ErlangErrorReasons.BadArgument,
                _ => throw new InvalidDataException(DriverParentWireNames422.Invalid)
            });
        }
        if (row.Closing && !row.PhysicallyStopped) await Close(row, transport).ConfigureAwait(false);
        owner.Cancellation.ThrowIfCancellationRequested();

        return identity;
    }

    internal void Stopped(ErlDriverPortIdentity420 identity, ErlDriverStopped420 stopped)
    {
        lock (gate)
        {
            var row = Find(identity) ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
            if (row.PhysicallyStopped) throw new InvalidDataException(DriverParentWireNames422.Invalid);
            row.PhysicallyStopped = true;
            row.Closing = true;
            row.CleanupStatus = stopped.CleanupStatus;
            // Release the real image port reference exactly once, even if the
            // final event arrived before the corresponding Open response.
            if (!row.NativeReferenceReleased)
            {
                row.NativeReferenceReleased = true;
                drivers.PortStopped(identity.Driver, identity.Port);
            }
            if (row.OpenResultKnown) FinishStopped(row);
        }
    }

    private void FinishStopped(NativeDriverPortRow422 row)
    {
        if (row.ChildAttempt455 is { } attempt) attempt.Row = null;
        native.Remove(row.Native);
        ports.Remove(row.Identity);
        row.Writable.TrySetResult();
        try
        {
            if (row.OpenSucceeded) runtime.PortRemoved(row.Identity, row.Links.ToArray(), row.Reason);
            if (row.CleanupStatus != 0) throw new IOException(NativeDriverPortNames422.NativeCleanupFailed);
            row.Stopped.TrySetResult();
        }
        catch (Exception failure)
        {
            row.Stopped.TrySetException(failure);
        }
        finally
        {
            row.Links.Clear();
            row.Data = Nil.Value;
            row.Reason = Nil.Value;
            row.Lines = null;
            row.Command = [];
        }
    }

    private async Task Close(NativeDriverPortRow422 row, DriverParentPeerTransport422 transport)
    {
        bool send;
        lock (gate)
        {
            send = !row.PhysicallyStopped;
            row.Closing = true;
            row.Writable.TrySetResult();
        }
        if (send) await transport.Close(row.Native).ConfigureAwait(false);
        // Close response releases its normal permit before delayed cleanup jobs
        // are awaited. Those jobs can require that exact normal queue.
        await row.Stopped.Task.ConfigureAwait(false);
    }
}
