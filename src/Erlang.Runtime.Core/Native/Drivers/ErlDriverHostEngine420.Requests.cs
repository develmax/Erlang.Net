namespace Erlang;

internal sealed partial class ErlDriverHostEngine420
{
    internal async ValueTask<ErlDriverHostFrame420> Handle(ErlDriverHostFrame420 request, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (request.Kind != ErlDriverHostKind420.Request || request.Name is not null && request.Operation != ErlDriverHostOperation420.Load)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        byte[]? data = null;
        int result = 0;
        switch (request.Operation)
        {
            case ErlDriverHostOperation420.Load:
                {
                    RequireMetadata(request, hasPort: false, hasActor: false);
                    if (request.Name is null) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    lock (gate) ObjectDisposedException.ThrowIf(closing, this);
                    var loadInput = ErlDriverPrimitiveWire420.Decode<ErlDriverLoadRequest420>(request.Data);
                    result = await images.LoadAsync(
                        request.Driver,
                        request.Name,
                        loadInput.NativePath,
                        request.SchedulerThread
                    ).ConfigureAwait(false);
                    if (result == ErlDriverLoadErrors420.NoError)
                        data = ErlDriverPrimitiveWire420.Encode(new ErlDriverLoadedEntry420(unchecked((uint)images.PortSlot(request.Driver).Entry.Flags)));
                    break;
                }
            case ErlDriverHostOperation420.Unload:
                RequireMetadata(request, hasPort: false, hasActor: false);
                if (request.Data is not null) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                await images.UnloadAsync(request.Driver).ConfigureAwait(false);
                break;
            case ErlDriverHostOperation420.OpenPort:
                RequireMetadata(request, hasPort: true, hasActor: true);
                if (!request.SchedulerThread) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                data = ErlDriverPrimitiveWire420.Encode(Open(request));
                break;
            case ErlDriverHostOperation420.Output:
            case ErlDriverHostOperation420.Control:
            case ErlDriverHostOperation420.Call:
                if (request.Driver == 0 || request.Port == 0 || request.Actor == 0 || !request.SchedulerThread)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                if (request.Operation == ErlDriverHostOperation420.Output && request.Command != 0)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                var input = ErlDriverPrimitiveWire420.Decode<ErlDriverActorRequest420>(request.Data);
                if (ExternalTermFormat.Decode(input.ActorIdentityEtf) is not Pid actor)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                nuint caller = tokens.Actor(request.Actor, actor);
                try
                {
                    var port = ResolveOrdinary(request);
                    await port.Construction455.ConfigureAwait(false);
                    switch (request.Operation)
                    {
                        case ErlDriverHostOperation420.Output:
                            port.Output(input.Bytes, caller, request.SchedulerThread);
                            break;
                        case ErlDriverHostOperation420.Control:
                            data = ErlDriverPrimitiveWire420.Encode(port.Control(
                                request.Command,
                                input.Bytes,
                                caller,
                                request.SchedulerThread
                            ));
                            break;
                        default:
                            data = port.Call(
                                request.Command,
                                input.Bytes,
                                caller,
                                request.SchedulerThread
                            );
                            break;
                    }
                }
                catch (ErlDriverSourceBadArgument420)
                {
                    result = -1;
                    data = null;
                }
                break;
            case ErlDriverHostOperation420.DistributionOutput457:
                RequireMetadata(request, hasPort: true, hasActor: false);
                if (!request.SchedulerThread || request.Data is null)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                var distributionPort = ResolveOrdinary(request);
                await distributionPort.Construction455.ConfigureAwait(false);
                distributionPort.OutputDistribution457(request.Data);
                break;
            case ErlDriverHostOperation420.ClosePort:
                RequireMetadata(request, hasPort: true, hasActor: false);
                if (request.Data is not null || !request.SchedulerThread)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                ErlDriverPort420? closingPort;
                lock (gate) ports.TryGetValue(request.Port, out closingPort);
                if (closingPort is not null && closingPort.Identity.Driver != request.Driver)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                using (new ErlDriverThreadRole420(request.SchedulerThread)) closingPort?.RequestStop();
                break;
            case ErlDriverHostOperation420.ProcessExited:
                RequireMetadata(request, hasPort: true, hasActor: false);
                await monitors!.ProcessExited(new(request.Driver, request.Port),
                    ErlDriverPrimitiveWire420.Decode<ulong>(request.Data)).ConfigureAwait(false);
                break;
            case ErlDriverHostOperation420.InvokePortTask:
                await WaitConstruction455(new(request.Driver, request.Port), (ErlDriverPortTaskKind420)request.Command).ConfigureAwait(false);

                return jobs!.Invoke(request);
            case ErlDriverHostOperation420.PortConnected:
                if (request.Driver == 0 || request.Port == 0 || request.Actor == 0 || request.Command != 0)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                var connection = ErlDriverPrimitiveWire420.Decode<ErlDriverActorRequest420>(request.Data);
                if (connection.Bytes.Length != 0 || ExternalTermFormat.Decode(connection.ActorIdentityEtf) is not Pid connected)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                ResolveOrdinary(request).Connected = tokens.Actor(request.Actor, connected);
                break;
            case ErlDriverHostOperation420.Shutdown:
                if (request.Driver != 0 || request.Port != 0 || request.Actor != 0 || request.Command != 0
                    || request.Data is not null || request.SchedulerThread)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                await CloseAsync().ConfigureAwait(false);
                break;
            default:
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        }
        if (fault.Failure is { } failure) throw new IOException(ErlDriverHostNames420.Closed, failure);

        return request with { Kind = ErlDriverHostKind420.Response, NativeResult = result, Name = null, Data = data };
    }

    private static void RequireMetadata(ErlDriverHostFrame420 request, bool hasPort, bool hasActor)
    {
        if (request.Driver == 0 || hasPort != (request.Port != 0) || hasActor != (request.Actor != 0) || request.Command != 0)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }

    private ErlDriverPort420 ResolveOrdinary(ErlDriverHostFrame420 request)
    {
        lock (gate)
        {
            if (!ports.TryGetValue(request.Port, out var port)) throw new ErlDriverSourceBadArgument420();
            if (port.Identity.Driver != request.Driver) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            if (closing || port.Closing) throw new ErlDriverSourceBadArgument420();

            return port;
        }
    }

    private ErlDriverOpenResult420 Open(ErlDriverHostFrame420 request)
    {
        var input = ErlDriverPrimitiveWire420.Decode<ErlDriverOpenRequest420>(request.Data);
        if (ExternalTermFormat.Decode(input.PortIdentityEtf) is not PortTerm identity
            || ExternalTermFormat.Decode(input.ConnectedIdentityEtf) is not Pid connected)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var slot = linked!.PortSlot(request.Driver) ?? images.PortSlot(request.Driver);
        nuint portTerm = tokens.Port(request.Port, identity);
        nuint actor = tokens.Actor(request.Actor, connected);
        ErlDriverPort420 port;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (request.Port == 0 || request.Port > (ulong)nint.MaxValue || !admittedPortIds455.Add(request.Port))
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            ErlDriverPortRetirement420? preparedRetirement = null;
            port = new(
                checked((nint)request.Port),
                new(request.Driver, request.Port),
                input.OpenFlags,
                input.SoftEof,
                slot,
                memory!,
                layout,
                start!,
                portTerm,
                actor,
                _ => preparedRetirement?.Stop(),
                select!.MissingReady
            );
            try
            {
                preparedRetirement = new(port, RetirePort);
                retirement.Add(preparedRetirement.Completion);
                ports.Add(request.Port, port);
                try
                {
                    termPorts.Add(portTerm, port);
                }
                catch
                {
                    ports.Remove(request.Port);
                    throw;
                }
            }
            catch
            {
                port.Dispose();
                throw;
            }
        }
        var started = port.Start(input.CommandBytes, actor, request.SchedulerThread);

        return new(started.Failed ? checked((int)started.Data) : 0, started.NativeErrno);
    }
}
