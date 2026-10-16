namespace Erlang.Tests;

public static class ConnectedSpawnRetirementHostChecks
{
    public static async Task StaleReplyCannotRebind()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        b.Runtime.Modules.Register(
            ConnectedSpawnHostNames.TargetModule,
            ConnectedSpawnHostNames.TargetFunction,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );

        async Task<ReferenceTerm> Request() => (ReferenceTerm)await caller.InvokeAsync(async context =>
            await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                Nil.Value
            ));

        ReferenceTerm oldReference = await Request();
        byte[] oldRequest = await a.TakePacket();
        TupleTerm oldHandle = a.Handle!;
        await a.Reconnect(b);
        if (oldHandle.Equals(a.Handle))
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        await caller.InvokeAsync(async context =>
        {
            var failure = await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var reference, Atom { Name: ProcessSpawnRequestNames.Error }, Atom { Name: NodeSpawnNames.Disconnected }] } tuple &&
                reference.Equals(oldReference) ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            );
            if (failure is null)
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);

            return true;
        });
        await b.PutPacket(oldRequest);
        byte[] oldReply = await b.TakePacket();
        await a.PutPacket(oldReply);
        await caller.InvokeAsync(async context =>
        {
            if (await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);

            return true;
        });

        ReferenceTerm currentReference = await Request();
        if (currentReference.Equals(oldReference))
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        await b.PutPacket(await a.TakePacket());
        await a.PutPacket(await b.TakePacket());
        await caller.InvokeAsync(async context =>
        {
            var success = await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var reference, Atom { Name: ConnectedSpawnHostNames.Success }, Pid remote] } tuple &&
                reference.Equals(currentReference) && remote.Node == b.Runtime.Node ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            );
            if (success is null || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);

            return true;
        });
    }
}
