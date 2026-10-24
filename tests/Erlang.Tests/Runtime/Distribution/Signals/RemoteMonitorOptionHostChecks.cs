namespace Erlang.Tests;

public static class RemoteMonitorOptionHostChecks
{
    public static async Task Run()
    {
        await RunSpawnOpt();
        await RunSpawnRequest();
    }

    public static async Task RunSpawnOpt()
    {
        foreach (bool customLast in new[] { false, true })
            await RunCase(false, customLast);
        await RunCase(false, false, emptyTupleLast: true);
    }

    public static async Task RunSpawnRequest()
    {
        foreach (bool customLast in new[] { false, true })
            await RunCase(true, customLast);
        await RunCase(true, false, emptyTupleLast: true);
    }

    private static async Task RunCase(bool request, bool customLast, bool emptyTupleLast = false)
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(RemoteMonitorOptionNames.NodeA);
        await b.Initialize(RemoteMonitorOptionNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var entered = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var left = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        b.Runtime.Modules.Register(
            RemoteMonitorOptionNames.Module,
            RemoteMonitorOptionNames.Child,
            0,
            async (context, _) =>
            {
                entered.TrySetResult(context.Self);
                try
                {
                    await context.ReceiveAsync(value => value.Equals(Term.A(RemoteMonitorOptionNames.Release)) ? value : null);

                    return Nil.Value;
                }
                finally
                {
                    left.TrySetResult();
                }
            }
        );

        Term plain = Term.A(RemoteMonitorOptionNames.Monitor);
        Term custom = Term.Tuple(plain, Term.List(Term.Tuple(Term.A(RemoteMonitorOptionNames.Tag), Term.A(RemoteMonitorOptionNames.Custom))));
        Term options = emptyTupleLast
            ? Term.List(custom, Term.Tuple(plain, Nil.Value))
            : customLast ? Term.List(plain, custom) : Term.List(custom, plain);
        Pid? child = null;
        try
        {
            Task<Term> pending = caller.InvokeAsync(async context => await a.Runtime.Modules.Call(
                context,
                RemoteMonitorOptionNames.Erlang,
                request ? RemoteMonitorOptionNames.Request : RemoteMonitorOptionNames.Spawn,
                Term.A(b.Runtime.Node),
                Term.A(RemoteMonitorOptionNames.Module),
                Term.A(RemoteMonitorOptionNames.Child),
                Nil.Value,
                options
            ));
            byte[] requestBytes = await a.TakePacket();
            var sent = DistributionPassThroughCodec.DecodeProcessPacket(requestBytes, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(sent.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnRequest)));
            Require(sent.Control.Items[DistributionSpawnLayout.OptionsIndex].Equals(emptyTupleLast ? Term.List(plain) : Term.List(plain, plain)));
            ReferenceTerm reference = (ReferenceTerm)sent.Control.Items[DistributionSpawnLayout.ReferenceIndex];
            await b.PutPacket(requestBytes);
            Pid spawned = await entered.Task.WaitAsync(RemoteMonitorOptionNames.Deadline);
            child = spawned;
            Require(spawned.Node == b.Runtime.Node && spawned.Node != a.Runtime.Node);
            byte[] replyBytes = await b.TakePacket();
            var reply = DistributionPassThroughCodec.DecodeProcessPacket(replyBytes, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(reply.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnReply)));
            Require(reply.Control.Items[DistributionSpawnLayout.ReplyResultIndex].Equals(spawned));
            await a.PutPacket(replyBytes);
            Term started = await pending.WaitAsync(RemoteMonitorOptionNames.Deadline);
            Require(request ? started.Equals(reference) : started.Equals(Term.Tuple(spawned, reference)));
            b.Runtime.Send(spawned, Term.A(RemoteMonitorOptionNames.Release));
            byte[] deathBytes = await b.TakePacket();
            var death = DistributionPassThroughCodec.DecodeProcessPacket(deathBytes, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(death.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.MonitorExit)));
            await a.PutPacket(deathBytes);
            await caller.InvokeAsync(async context =>
            {
                if (request)
                {
                    Term? spawnReply = await context.ReceiveAsync(
                        value => value is TupleTerm { Items: [Atom { Name: RemoteMonitorOptionNames.Reply }, _, _, _] } ? value : null,
                        RemoteMonitorOptionNames.Deadline
                    );
                    Require(spawnReply is not null && spawnReply.Equals(Term.Tuple(
                        Term.A(RemoteMonitorOptionNames.Reply),
                        reference,
                        Term.A(RemoteMonitorOptionNames.Success),
                        spawned
                    )));
                }
                Term? down = await context.ReceiveAsync(
                    value => value is TupleTerm { Items.Count: 5 } ? value : null,
                    RemoteMonitorOptionNames.Deadline
                );
                Require(down is not null && down.Equals(Term.Tuple(
                    Term.A(emptyTupleLast ? RemoteMonitorOptionNames.Down : RemoteMonitorOptionNames.Custom),
                    reference,
                    Term.A(RemoteMonitorOptionNames.Process),
                    spawned,
                    Term.A(RemoteMonitorOptionNames.Normal)
                )));

                return Nil.Value;
            });
        }
        finally
        {
            if (child is not null)
            {
                b.Runtime.Send(child, Term.A(RemoteMonitorOptionNames.Release));
                await left.Task.WaitAsync(RemoteMonitorOptionNames.Deadline);
            }
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RemoteMonitorOptionNames.Failure);
    }
}
