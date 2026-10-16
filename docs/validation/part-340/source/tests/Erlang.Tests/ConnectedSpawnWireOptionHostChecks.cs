namespace Erlang.Tests;

public static class ConnectedSpawnWireOptionHostChecks
{
    public static async Task WireOrderAndLastTuplePrecedence()
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
            Term options = Term.List(
                Term.A(ConnectedSpawnHostNames.Monitor),
                Term.Tuple(
                    Term.A(ConnectedSpawnHostNames.Monitor),
                    Term.List(
                    Term.Tuple(Term.A(ConnectedSpawnOptionHostNames.Tag), Term.A(ConnectedSpawnOptionHostNames.EarlierDown)))
                ),
                Term.Tuple(Term.A(ConnectedSpawnHostNames.Link), Term.List(Term.A(ConnectedSpawnOptionHostNames.Priority))),
                Term.Tuple(
                    Term.A(ConnectedSpawnHostNames.Monitor),
                    Term.List(
                        Term.A(ConnectedSpawnOptionHostNames.Priority),
                        Term.Tuple(Term.A(ConnectedSpawnOptionHostNames.Tag), Term.A(ConnectedSpawnOptionHostNames.CustomDown))
                    )
                ),
                Term.A(ConnectedSpawnHostNames.Monitor),
                Term.Tuple(Term.A(ConnectedSpawnHostNames.Link), Nil.Value),
                Term.A(ConnectedSpawnHostNames.Link),
                Term.Tuple(Term.A(ProcessSpawnRequestNames.ReplyTag), Term.A(ConnectedSpawnOptionHostNames.CustomReply)),
                Term.Tuple(Term.A(ProcessSpawnRequestNames.Reply), Term.A(ProcessSpawnRequestNames.Yes))
            );

            return await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                options
            );
        });
        byte[] request = await a.TakePacket();
        var decoded = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(decoded.Control.Items[DistributionSpawnLayout.OptionsIndex].Equals(Term.List(
            Term.A(ConnectedSpawnHostNames.Link),
            Term.A(ConnectedSpawnHostNames.Monitor),
            Term.A(ConnectedSpawnHostNames.Monitor),
            Term.A(ConnectedSpawnHostNames.Monitor),
            Term.A(ConnectedSpawnHostNames.Link)
        )));
        await b.PutPacket(request);
        Pid child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        await a.PutPacket(await b.TakePacket());
        await caller.InvokeAsync(async context =>
        {
            var response = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            Require(response!.Equals(Term.Tuple(
                Term.A(ConnectedSpawnOptionHostNames.CustomReply),
                reference,
                Term.A(ConnectedSpawnHostNames.Success),
                child
            )));
            a.Runtime.Send(context.Self, Term.A(ConnectedSpawnHostNames.Unrelated));

            return true;
        });
        b.Runtime.Send(child, Term.A(ConnectedSpawnHostNames.Release));
        await a.PutPacket(await b.TakePacket());
        await a.PutPacket(await b.TakePacket());
        await caller.InvokeAsync(async context =>
        {
            Term? first = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            Term? second = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            var values = new[] { first, second };
            Require(values.Any(value => value!.Equals(Term.Tuple(Term.A(ConnectedSpawnHostNames.Exit), child, Term.A(ConnectedSpawnHostNames.ChildReason)))));
            Require(values.Any(value => value!.Equals(Term.Tuple(
                Term.A(ConnectedSpawnOptionHostNames.CustomDown),
                reference,
                Term.A(ProcessMonitorKinds.Process),
                child,
                Term.A(ConnectedSpawnHostNames.ChildReason)
            ))));
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
