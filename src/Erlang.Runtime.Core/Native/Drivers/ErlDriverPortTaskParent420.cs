namespace Erlang;

internal sealed class ErlDriverPortTaskParent420
{
    private readonly ErlDriverHostPeer420 peer;
    private readonly IErlDriverPortTaskParent420 dispatcher;

    internal ErlDriverPortTaskParent420(ErlDriverHostPeer420 peer, IErlDriverPortTaskParent420 dispatcher)
    {
        this.peer = peer;
        this.dispatcher = dispatcher;
    }

    internal async ValueTask<ErlDriverHostFrame420> Handle(ErlDriverHostFrame420 request)
    {
        if (request.Operation != ErlDriverHostOperation420.SchedulePortTask || request.Actor != 0
            || request.Name is not null || request.SchedulerThread
            || !Enum.IsDefined((ErlDriverPortTaskKind420)request.Command))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        ulong job = ErlDriverPrimitiveWire420.Decode<ulong>(request.Data);
        if (job == 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        await dispatcher.Dispatch(
            new(request.Driver, request.Port),
            async () =>
        {
            var result = await peer.RequestAsync(
                ErlDriverHostOperation420.InvokePortTask,
                request.Driver,
                request.Port,
                0,
                request.Command,
                null,
                ErlDriverPrimitiveWire420.Encode(job),
                CancellationToken.None,
                schedulerThread: true
            ).ConfigureAwait(false);
            if (result.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        }
        ).ConfigureAwait(false);

        return request with { Kind = ErlDriverHostKind420.Response, NativeResult = 0, Name = null, Data = null };
    }
}
