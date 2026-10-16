namespace Erlang.Tests;

public static class ConnectedSpawnPriorityLinkHostChecks
{
    public static async Task PriorityLinkedExit()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var entered = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
        b.Runtime.Modules.Register(
            ConnectedSpawnHostNames.TargetModule,
            ConnectedSpawnHostNames.TargetFunction,
            0,
            async (context, _) =>
            {
                entered.TrySetResult(context.Self);
                await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnHostNames.Release)) ? value : null);
                context.Exit(Term.A(ConnectedSpawnHostNames.ChildReason));

                return Nil.Value;
            }
        );
        var reference = (ReferenceTerm)await caller.InvokeAsync(async context =>
        {
            context.TrapExits = true;
            a.Runtime.Send(context.Self, Term.A(ConnectedSpawnHostNames.Unrelated));

            return await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                Term.List(Term.Tuple(Term.A(ConnectedSpawnHostNames.Link), Term.List(Term.A(ConnectedSpawnOptionHostNames.Priority))))
            );
        });
        await b.PutPacket(await a.TakePacket());
        Pid child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        await a.PutPacket(await b.TakePacket());
        await caller.InvokeAsync(async context =>
        {
            var reply = await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var actual, _, _] } tuple &&
                actual.Equals(reference) ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            );
            Require(reply is not null);

            return true;
        });
        b.Runtime.Send(child, Term.A(ConnectedSpawnHostNames.Release));
        await a.PutPacket(await b.TakePacket());
        await caller.InvokeAsync(async context =>
        {
            Term? first = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            Require(first!.Equals(Term.Tuple(Term.A(ConnectedSpawnHostNames.Exit), child, Term.A(ConnectedSpawnHostNames.ChildReason))));
            Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!.Equals(Term.A(ConnectedSpawnHostNames.Unrelated)));

            return true;
        });
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
