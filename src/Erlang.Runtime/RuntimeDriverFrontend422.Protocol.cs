namespace Erlang;

internal sealed partial class RuntimeDriverFrontend422
{
    private async ValueTask<ErlDriverHostFrame420> Handle(ErlDriverHostFrame420 request,
        CancellationToken cancellation)
    {
        // Peer has already authenticated session, ownership IDs and the ordered
        // event prefix. A nonzero admitted prefix is valid on reverse requests.
        if (request.Kind != ErlDriverHostKind420.Request)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        if (request.Operation == ErlDriverHostOperation420.PrimitiveHost)
            return await primitives.Handle(request, cancellation).ConfigureAwait(false);
        if (request.Operation == ErlDriverHostOperation420.SchedulePortTask)
            return await tasks.Handle(request).ConfigureAwait(false);
        if (request.Actor != 0 || request.Name is not null || request.SchedulerThread)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        int result = 0;
        byte[]? output = null;
        var identity = new ErlDriverPortIdentity420(request.Driver, request.Port);
        switch (request.Operation)
        {
            case ErlDriverHostOperation420.RegisterLinkedEntry:
                {
                    if (request.Port != 0 || request.Command != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    var entry = ErlDriverPrimitiveWire420.Decode<ErlDriverLinkedEntryRequest420>(request.Data);
                    if (entry.OwnerImage != request.Driver) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    output = ErlDriverPrimitiveWire420.Encode(drivers.RegisterLinked(entry));
                    break;
                }
            case ErlDriverHostOperation420.RemoveLinkedEntry:
                if (request.Port != 0 || request.Command != 0 || request.Data is not null)
                    throw new InvalidDataException(DriverParentWireNames422.Invalid);
                result = drivers.RemoveLinked(request.Driver);
                if (result != 1) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                result = 0;
                break;
            case ErlDriverHostOperation420.PortFailure:
                {
                    RequirePortRequest(request);
                    var failure = ErlDriverPrimitiveWire420.Decode<ErlDriverFailureRequest420>(request.Data);
                    result = ports.Failure(
                        identity,
                        ExternalTermFormat.Decode(failure.ReasonEtf),
                        failure.Eof,
                        failure.Unlink
                    );
                    break;
                }
            case ErlDriverHostOperation420.PortBusy:
                if (request.Driver == 0 || request.Port == 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                switch (request.Command)
                {
                    case 0: ports.SetBusy(identity, ErlDriverPrimitiveWire420.Decode<bool>(request.Data)); break;
                    case 1:
                        var limits = ErlDriverPrimitiveWire420.Decode<ErlDriverLimitsRequest420>(request.Data);
                        output = ErlDriverPrimitiveWire420.Encode(ports.BusyLimits(identity, limits.Low, limits.High));
                        break;
                    case 2: result = ports.ConsumeTimeslice(identity, ErlDriverPrimitiveWire420.Decode<int>(request.Data)); break;
                    default: throw new InvalidDataException(DriverParentWireNames422.Invalid);
                }
                break;
            case ErlDriverHostOperation420.LockDriver:
                RequirePortRequest(request);
                if (request.Data is not null) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                result = ports.LockDriver(identity);
                break;
            case ErlDriverHostOperation420.MonitorProcess:
                {
                    RequirePortRequest(request);
                    var monitor = ErlDriverPrimitiveWire420.Decode<ErlDriverMonitorRequest420>(request.Data);
                    if (ExternalTermFormat.Decode(monitor.TargetIdentityEtf) is not Pid target)
                        throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    output = ErlDriverPrimitiveWire420.Encode(monitors.Register(
                        identity,
                        monitor.Attempt,
                        monitor.TargetActor,
                        target
                    ));
                    break;
                }
            case ErlDriverHostOperation420.DemonitorProcess:
                RequirePortRequest(request);
                result = monitors.Unregister(identity, ErlDriverPrimitiveWire420.Decode<ulong>(request.Data));
                break;
            case ErlDriverHostOperation420.PortTermOutput:
                {
                    if (request.Command != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    var message = ErlDriverPrimitiveWire420.Decode<ErlDriverTermDelivery420>(request.Data);
                    Pid recipient = actors.Resolve(message.RecipientActor);
                    if (ExternalTermFormat.Decode(message.RecipientEtf) is not Pid wire || wire.Id != recipient.Id)
                        throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    Term? sender = null;
                    if (!message.UndefinedSender) sender = ports.OriginIdentity(identity);
                    else if (request.Driver != 0 || request.Port != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
                    result = runtime.DeliverDriverTerm422(recipient, message.MessageEtf, sender);
                    break;
                }
            default: throw new InvalidDataException(DriverParentWireNames422.Invalid);
        }

        return request with { Kind = ErlDriverHostKind420.Response, NativeResult = result, Name = null, Data = output };
    }

    private ValueTask Event(ErlDriverHostFrame420 message, CancellationToken cancellation)
    {
        if (message.Kind != ErlDriverHostKind420.Event || message.Driver == 0 || message.Port == 0
            || message.Actor != 0 || message.Command != 0 || message.Name is not null
            || message.SchedulerThread || message.NativeResult != 0)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        var identity = new ErlDriverPortIdentity420(message.Driver, message.Port);
        switch (message.Operation)
        {
            case ErlDriverHostOperation420.PortOutput:
                ports.Output(identity, ErlDriverPrimitiveWire420.Decode<ErlDriverOutput420>(message.Data));
                break;
            case ErlDriverHostOperation420.PortStopped:
                monitors.PortStopped(identity);
                ports.Stopped(identity, ErlDriverPrimitiveWire420.Decode<ErlDriverStopped420>(message.Data));
                break;
            default: throw new InvalidDataException(DriverParentWireNames422.Invalid);
        }

        return ValueTask.CompletedTask;
    }

    private static void RequirePortRequest(ErlDriverHostFrame420 request)
    {
        if (request.Driver == 0 || request.Port == 0 || request.Command != 0)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }
}
