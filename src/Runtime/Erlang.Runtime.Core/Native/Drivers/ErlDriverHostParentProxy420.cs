namespace Erlang;

internal sealed class ErlDriverHostParentProxy420 : IErlDriverPortHost420, IErlDriverMonitorHost420, IErlDriverLinkedEntryHost420
{
    private readonly ErlDriverHostPeer420 peer;

    internal ErlDriverHostParentProxy420(ErlDriverHostPeer420 peer) => this.peer = peer;

    private ErlDriverHostFrame420 Request(
        ErlDriverHostOperation420 operation,
        ErlDriverPortIdentity420 port,
        uint command = 0,
        byte[]? data = null
    )
        => peer.RequestAsync(
            operation,
            port.Driver,
            port.Port,
            0,
            command,
            null,
            data,
            CancellationToken.None
        ).AsTask().GetAwaiter().GetResult();

    public int Failure(
        ErlDriverPortIdentity420 port,
        Term reason,
        bool eof,
        bool unlink
    )
        => Request(
            ErlDriverHostOperation420.PortFailure,
            port,
            data: ErlDriverPrimitiveWire420.Encode(
            new ErlDriverFailureRequest420(ExternalTermFormat.Encode(reason), eof, unlink))
        ).NativeResult;

    public void SetBusy(ErlDriverPortIdentity420 port, bool busy)
    {
        var result = Request(
            ErlDriverHostOperation420.PortBusy,
            port,
            0,
            ErlDriverPrimitiveWire420.Encode(busy)
        );
        RequireSuccess(result);
    }

    public ErlDriverBusyLimits420 BusyLimits(ErlDriverPortIdentity420 port, ulong? low, ulong? high)
    {
        var result = Request(
            ErlDriverHostOperation420.PortBusy,
            port,
            1,
            ErlDriverPrimitiveWire420.Encode(new ErlDriverLimitsRequest420(low, high))
        );
        RequireSuccess(result);

        return ErlDriverPrimitiveWire420.Decode<ErlDriverBusyLimits420>(result.Data);
    }

    public int ConsumeTimeslice(ErlDriverPortIdentity420 port, int percent)
        => Request(
            ErlDriverHostOperation420.PortBusy,
            port,
            2,
            ErlDriverPrimitiveWire420.Encode(percent)
        ).NativeResult;

    public int LockDriver(ErlDriverPortIdentity420 port)
        => Request(ErlDriverHostOperation420.LockDriver, port).NativeResult;

    public ErlDriverMonitorRegistration420 Register(
        ErlDriverPortIdentity420 port,
        ulong attempt,
        ulong targetActor,
        Pid target
    )
    {
        var result = Request(
            ErlDriverHostOperation420.MonitorProcess,
            port,
            data:
            ErlDriverPrimitiveWire420.Encode(new ErlDriverMonitorRequest420(attempt, targetActor, ExternalTermFormat.Encode(target)))
        );
        RequireSuccess(result);

        return ErlDriverPrimitiveWire420.Decode<ErlDriverMonitorRegistration420>(result.Data);
    }

    public int Unregister(ErlDriverPortIdentity420 port, ulong attempt)
        => Request(ErlDriverHostOperation420.DemonitorProcess, port,
            data: ErlDriverPrimitiveWire420.Encode(attempt)).NativeResult;

    public string Session438 => peer.Session438;

    public ulong Register(ErlDriverLinkedEntryRequest420 entry, ErlDriverLinkedScope438 scope)
    {
        var result = Request(
            ErlDriverHostOperation420.RegisterLinkedEntry,
            new(entry.OwnerImage, 0),
            data: ErlDriverPrimitiveWire420.Encode(new ErlDriverLinkedRegistration438(entry, scope))
        );
        RequireSuccess(result);

        return ErlDriverPrimitiveWire420.Decode<ulong>(result.Data);
    }

    public void Remove(ulong identity, ErlDriverLinkedScope438 scope)
        => RequireSuccess(Request(ErlDriverHostOperation420.RemoveLinkedEntry, new(identity, 0),
            data: ErlDriverPrimitiveWire420.Encode(scope)));

    public ErlDriverLinkedScope438 BeginInitialization438(ulong ownerImage)
    {
        var result = Request(ErlDriverHostOperation420.BeginLinkedInitialization438, new(ownerImage, 0));
        RequireSuccess(result);
        var scope = ErlDriverPrimitiveWire420.Decode<ErlDriverLinkedScope438>(result.Data);
        if (!StringComparer.Ordinal.Equals(scope.Session, peer.Session438) || scope.OwnerImage != ownerImage || scope.Token == 0)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return scope;
    }

    public void EndInitialization438(ErlDriverLinkedScope438 scope, bool cleanupSucceeded)
        => RequireSuccess(Request(
            ErlDriverHostOperation420.EndLinkedInitialization438,
            new(scope.OwnerImage, 0),
            data: ErlDriverPrimitiveWire420.Encode(new ErlDriverLinkedEnd438(scope, cleanupSucceeded))
        ));

    private static void RequireSuccess(ErlDriverHostFrame420 response)
    {
        if (response.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }
}
