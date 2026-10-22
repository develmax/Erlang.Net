namespace Erlang;

// Every scheduler=true request is admitted by the real retained auxiliary lane.
// Native finish/unload uses the source non-scheduler role. Unrelated modules do
// not gain a scheduler-thread bypass, and actor cancellation never abandons an
// already accepted child response/created native owner.
internal sealed class DriverParentPeerTransport422
{
    private readonly ProcessRuntime runtime;
    private readonly ErlDriverHostPeer420 peer;
    private readonly DriverParentActorHandles422 actors;
    private readonly DriverDispatcherLease422 dispatcher;
    private Func<ErlDriverPortIdentity420, Func<Task>, Task>? portDispatcher;

    internal DriverParentPeerTransport422(
        ProcessRuntime runtime,
        ErlDriverHostPeer420 peer,
        DriverParentActorHandles422 actors,
        DriverDispatcherLease422 dispatcher
    )
    {
        this.runtime = runtime;
        this.peer = peer;
        this.actors = actors;
        this.dispatcher = dispatcher;
    }

    internal void BindPortDispatcher(Func<ErlDriverPortIdentity420, Func<Task>, Task> value)
    {
        if (Interlocked.CompareExchange(ref portDispatcher, value, null) is not null)
            throw new InvalidOperationException(DriverParentWireNames422.Invalid);
    }

    private async ValueTask<ErlDriverHostFrame420> Request(
        ErlDriverHostOperation420 operation,
        ulong driver,
        ulong port,
        ulong actor,
        uint command,
        byte[]? name,
        byte[]? data,
        CancellationToken cancellation,
        bool scheduler
    )
    {
        cancellation.ThrowIfCancellationRequested();
        ErlDriverHostFrame420? response = null;
        Func<Task> invoke = async () =>
        {
            cancellation.ThrowIfCancellationRequested();
            DriverPortDispatchPermit422.Require(runtime);
            response = await peer.RequestAsync(
                operation,
                driver,
                port,
                actor,
                command,
                name,
                data,
                cancellation,
                schedulerThread: scheduler
            ).ConfigureAwait(false);
        };
        if (port != 0 && scheduler)
        {
            var schedule = portDispatcher ?? throw new InvalidOperationException(DriverParentWireNames422.Invalid);
            await schedule(new(driver, port), invoke).ConfigureAwait(false);
        }
        else await runtime.DispatchDriverPort422(dispatcher, invoke).ConfigureAwait(false);

        return response ?? throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }

    internal async ValueTask<DriverParentLoadResult422> Load(
        ulong driver,
        ErlDriverName420 name,
        ErlDriverPath420 path,
        CancellationToken cancellation
    )
    {
        var reply = await Request(
            ErlDriverHostOperation420.Load,
            driver,
            0,
            0,
            0,
            name.Bytes.ToArray(),
            ErlDriverPrimitiveWire420.Encode(new ErlDriverLoadRequest420(path.CopyBytes())),
            cancellation,
            true
        ).ConfigureAwait(false);
        if (reply.NativeResult != 0)
        {
            if (reply.Data is not null) throw new InvalidDataException(DriverParentWireNames422.Invalid);

            return new(reply.NativeResult, 0);
        }
        var entry = ErlDriverPrimitiveWire420.Decode<ErlDriverLoadedEntry420>(reply.Data);

        return new(0, entry.Flags);
    }

    internal async ValueTask Unload(ulong driver)
    {
        var reply = await Request(
            ErlDriverHostOperation420.Unload,
            driver,
            0,
            0,
            0,
            null,
            null,
            CancellationToken.None,
            false
        ).ConfigureAwait(false);
        if (reply.NativeResult != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }

    internal async ValueTask<ErlDriverOpenResult420> Open(
        ErlDriverPortIdentity420 identity,
        PortTerm port,
        Pid connected,
        byte[] command,
        int flags,
        bool softEof,
        CancellationToken cancellation
    )
    {
        var request = new ErlDriverOpenRequest420(
            command,
            flags,
            softEof,
            runtime.EncodeDriverIdentity422(port),
            runtime.EncodeDriverIdentity422(connected)
        );
        var reply = await Request(
            ErlDriverHostOperation420.OpenPort,
            identity.Driver,
            identity.Port,
            actors.Get(connected),
            0,
            null,
            ErlDriverPrimitiveWire420.Encode(request),
            cancellation,
            true
        ).ConfigureAwait(false);
        if (reply.NativeResult != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return ErlDriverPrimitiveWire420.Decode<ErlDriverOpenResult420>(reply.Data);
    }

    private ValueTask<ErlDriverHostFrame420> ActorOperation(
        ErlDriverHostOperation420 operation,
        ErlDriverPortIdentity420 identity,
        Pid caller,
        uint command,
        byte[] bytes,
        CancellationToken cancellation
    )
    {
        var request = new ErlDriverActorRequest420(runtime.EncodeDriverIdentity422(caller), bytes);

        return Request(
            operation,
            identity.Driver,
            identity.Port,
            actors.Get(caller),
            command,
            null,
            ErlDriverPrimitiveWire420.Encode(request),
            cancellation,
            true
        );
    }

    internal async ValueTask Output(
        ErlDriverPortIdentity420 identity,
        Pid caller,
        byte[] bytes,
        CancellationToken cancellation
    )
    {
        var reply = await ActorOperation(
            ErlDriverHostOperation420.Output,
            identity,
            caller,
            0,
            bytes,
            cancellation
        ).ConfigureAwait(false);
        if (reply.NativeResult == -1 && reply.Data is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (reply.NativeResult != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
    }

    internal async ValueTask<ErlDriverControlResult420> Control(
        ErlDriverPortIdentity420 identity,
        Pid caller,
        uint command,
        byte[] bytes,
        CancellationToken cancellation
    )
    {
        var reply = await ActorOperation(
            ErlDriverHostOperation420.Control,
            identity,
            caller,
            command,
            bytes,
            cancellation
        ).ConfigureAwait(false);
        if (reply.NativeResult == -1 && reply.Data is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (reply.NativeResult != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return ErlDriverPrimitiveWire420.Decode<ErlDriverControlResult420>(reply.Data);
    }

    internal async ValueTask<byte[]> Call(
        ErlDriverPortIdentity420 identity,
        Pid caller,
        uint command,
        byte[] bytes,
        CancellationToken cancellation
    )
    {
        var reply = await ActorOperation(
            ErlDriverHostOperation420.Call,
            identity,
            caller,
            command,
            bytes,
            cancellation
        ).ConfigureAwait(false);
        if (reply.NativeResult == -1 && reply.Data is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (reply.NativeResult != 0 || reply.Data is null) throw new InvalidDataException(DriverParentWireNames422.Invalid);

        return reply.Data;
    }

    internal async ValueTask Close(ErlDriverPortIdentity420 identity)
    {
        var reply = await Request(
            ErlDriverHostOperation420.ClosePort,
            identity.Driver,
            identity.Port,
            0,
            0,
            null,
            null,
            CancellationToken.None,
            true
        ).ConfigureAwait(false);
        if (reply.NativeResult != 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
        // Final parent removal/EXIT is driven independently by PortStopped ACK.
    }
}
