namespace Erlang.Tests;

internal static class DistSpawn430HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DistSpawn430HostNames.Failure);
    }

    private static ValueTask<Term> Call(
        ProcessContext c,
        Term node,
        Term options,
        Term? kind = null
    )
     => c.Runtime.Modules.Call(
         c,
         DistSpawn430HostNames.Module,
         DistSpawn430HostNames.Function,
         node,
         Term.Tuple(Term.A(DistSpawn430HostNames.Target), Term.A(DistSpawn430HostNames.Run), Nil.Value),
         options,
         kind ?? Term.A(DistSpawn430HostNames.Request)
     );

    private static async ValueTask<Term?> Reply(ProcessContext c, Term reference)
     => await c.Mailbox.ReceiveAsync<Term>(v => v is TupleTerm { Items.Count: 4 } t && t.Items[1].Equals(reference) ? v : null, TimeSpan.Zero, c.Cancellation);

    internal static async Task Errors()
    {
        await using var runtime = new ProcessRuntime();
        RuntimeBootstrapDistSpawn430.Register(runtime, runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            ReferenceTerm id = (ReferenceTerm)await Call(c, Term.A(DistSpawn430HostNames.Remote), Nil.Value);
            Require((await Reply(c, id))!.Equals(Term.Tuple(
                Term.A(DistSpawn430HostNames.Default),
                id,
                Term.A(DistSpawn430HostNames.Error),
                Term.A(DistSpawn430HostNames.NoConnection)
            )));
            Term options = new Cons(
                Term.Tuple(Term.A(DistSpawn430HostNames.ReplyTag), Term.A(DistSpawn430HostNames.Tag)),
                new Cons(Term.Tuple(Term.A(DistSpawn430HostNames.Reply), Term.A(DistSpawn430HostNames.Bad)), Term.I(0))
            );
            id = (ReferenceTerm)await Call(c, Term.A(DistSpawn430HostNames.Remote), options);
            Require((await Reply(c, id))!.Equals(Term.Tuple(
                Term.A(DistSpawn430HostNames.Tag),
                id,
                Term.A(DistSpawn430HostNames.Error),
                Term.A(DistSpawn430HostNames.BadOptions)
            )));
            Require((await Call(
                c,
                Term.A(DistSpawn430HostNames.Remote),
                options,
                Term.A(DistSpawn430HostNames.Opt)
            )).Equals(Term.A(DistSpawn430HostNames.BadArgument)));
            id = (ReferenceTerm)await Call(
                c,
                Term.A(DistSpawn430HostNames.Remote),
                Term.List(Term.Tuple(Term.A(DistSpawn430HostNames.Reply), Term.A(DistSpawn430HostNames.No)))
            );
            Require(await Reply(c, id) is null);
            Require((await Call(c, Term.A(runtime.Node), Nil.Value)).Equals(Term.A(DistSpawn430HostNames.BadArgument)));

            return true;
        });
    }

    internal static async Task RoundTrip()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        RuntimeBootstrapDistSpawn430.Register(a.Runtime, a.Runtime.Modules);
        b.Runtime.Modules.Register(
            DistSpawn430HostNames.Target,
            DistSpawn430HostNames.Run,
            0,
            (_, _) => ValueTask.FromResult<Term>(Term.A(DistSpawn430HostNames.Payload))
        );
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var id = (ReferenceTerm)await caller.InvokeAsync(c => Call(c, Term.A(b.Runtime.Node), Nil.Value).AsTask());
        byte[] request = await a.TakePacket();
        var packet = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(packet.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnRequest)) && packet.Control.Items[1].Equals(id));
        await b.PutPacket(request);
        await a.PutPacket(await b.TakePacket());
        Term? reply = await caller.InvokeAsync(async c => await Reply(c, id));
        Require(reply is TupleTerm { Items: [Atom { Name: DistSpawn430HostNames.Default }, var reference, Atom { Name: DistSpawn430HostNames.Ok }, Pid child] } && reference.Equals(id) && child.Node == b.Runtime.Node);
    }

    internal static async Task AsyncBusy()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        RuntimeBootstrapDistSpawn430.Register(a.Runtime, a.Runtime.Modules);
        Pid remote = (await b.Controller.Ready).Self;
        var filler = DistributionPassThroughCodec.Send(new Pid(b.Runtime.Node, remote.Id, remote.Creation), Term.A(DistSpawn430HostNames.Filler));
        int count = 0;
        while (true)
        {
            try
            {
                a.Runtime.DistributionPending.EnqueueConnectedOutput(a.Handle!, filler);
                count++;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
        Require(count > 0);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var reference = (ReferenceTerm)await caller.InvokeAsync(async c =>
        {
            await c.Runtime.Modules.Call(
                c,
                DistSpawn430HostNames.Erlang,
                DistSpawn430HostNames.Flag,
                Term.A(DistSpawn430HostNames.AsyncFlag),
                Term.A(DistSpawn430HostNames.True)
            );

            return await Call(c, Term.A(b.Runtime.Node), Nil.Value);
        }).WaitAsync(ConnectedSpawnHostNames.Deadline);
        // Queue remains full: genuine native acceptance must not wait for its physical drain.
        Require(a.Runtime.DistributionPending.ConnectedSnapshot(a.Handle!).QueuedPackets == count);
        await caller.DisposeAsync();
        for (int i = 0; i < count; i++)
        {
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(await a.TakePacket(), negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(packet.Payload!.Equals(Term.A(DistSpawn430HostNames.Filler)));
        }
        var published = DistributionPassThroughCodec.DecodeProcessPacket(await a.TakePacket(), negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(published.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnRequest)) && published.Control.Items[1].Equals(reference));
    }

    internal static async Task Frontier()
    {
        await using var endpoint = new ConnectedSpawnHostEndpoint();
        await endpoint.Initialize(ConnectedSpawnHostNames.NodeA);
        RuntimeBootstrapDistSpawn430.Register(endpoint.Runtime, endpoint.Runtime.Modules);
        RuntimeBootstrapDistSpawn430.Register(endpoint.Runtime, endpoint.Runtime.Modules);
        await using var caller = new RuntimeNodeFoundationHostActor(endpoint.Runtime);
        await caller.InvokeAsync(async c =>
        {
            endpoint.Runtime.DistributionPending.BeginPending(Term.A(DistSpawn430HostNames.Remote));
            bool refused = false;
            try
            {
                await Call(c, Term.A(DistSpawn430HostNames.Remote), Nil.Value);
            }
            catch (NotSupportedException)
            {
                refused = true;
            }
            Require(refused);

            return true;
        });
        await using var foreign = new ProcessRuntime();
        bool wrong = false;
        try
        {
            RuntimeBootstrapDistSpawn430.Register(endpoint.Runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            wrong = true;
        }
        Require(wrong);
    }
}
