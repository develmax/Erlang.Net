namespace Erlang.Tests;

public static class ConnectedSpawnMonitorOptionHostChecks
{
    public static Task ExplicitAlias() => Exchange(ConnectedSpawnOptionHostNames.ExplicitAlias);

    public static Task DemonitorAlias() => Exchange(ConnectedSpawnOptionHostNames.DemonitorAlias);

    public static Task ReplyDemonitorAlias() => Exchange(ConnectedSpawnOptionHostNames.ReplyDemonitorAlias);

    public static Task DemonitorAfterSuccess() => Exchange(ConnectedSpawnOptionHostNames.DemonitorAlias, true);

    private static async Task Exchange(string mode, bool removeMonitor = false)
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
            a.Runtime.Send(context.Self, Term.A(ConnectedSpawnHostNames.Unrelated));
            Term flags = Term.List(
                Term.A(ConnectedSpawnOptionHostNames.Priority),
                Term.Tuple(Term.A(ConnectedSpawnOptionHostNames.Tag), Term.A(ConnectedSpawnOptionHostNames.CustomDown)),
                Term.Tuple(Term.A(ConnectedSpawnOptionHostNames.Alias), Term.A(mode))
            );

            return await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                Term.List(Term.Tuple(Term.A(ConnectedSpawnHostNames.Monitor), flags))
            );
        });
        await b.PutPacket(await a.TakePacket());
        Pid child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        var localMonitor = (ReferenceTerm)await b.Controller.InvokeAsync(async context =>
            await b.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.Monitor,
                Term.A(ProcessMonitorKinds.Process),
                child
            ));
        await a.PutPacket(await b.TakePacket());
        await caller.InvokeAsync(async context =>
        {
            var first = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            Require(first!.Equals(Term.A(ConnectedSpawnHostNames.Unrelated)));
            var reply = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            Require(reply is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var actual, Atom { Name: ConnectedSpawnHostNames.Success }, var target] } &&
                actual.Equals(reference) && target.Equals(child));
            a.Runtime.Send(context.Self, Term.A(ConnectedSpawnHostNames.Unrelated));
            a.Runtime.Send(reference, Term.A(ConnectedSpawnOptionHostNames.AliasMessage));
            Term? message = await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnOptionHostNames.AliasMessage)) ? value : null,
                ConnectedSpawnHostNames.Deadline);
            Require(message is not null);
            if (removeMonitor)
            {
                Term removed = await a.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnOptionHostNames.Demonitor,
                    reference,
                    Term.List(Term.A(ConnectedSpawnOptionHostNames.Info))
                );
                Require(removed.Equals(Term.A(ErlangBooleanAtoms.True)));
            }

            return true;
        });
        if (mode == ConnectedSpawnOptionHostNames.ReplyDemonitorAlias || removeMonitor)
        {
            byte[] demonitor = await a.TakePacket();
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(demonitor, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(packet.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.Demonitor)));
            await b.PutPacket(demonitor);
        }
        b.Runtime.Send(child, Term.A(ConnectedSpawnHostNames.Release));
        await b.Controller.InvokeAsync(async context =>
        {
            var down = await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Down }, var actual, _, _, _] } tuple &&
                actual.Equals(localMonitor) ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            );
            Require(down is not null && down.Items[^1].Equals(Term.A(ConnectedSpawnHostNames.ChildReason)));

            return true;
        });
        if (mode != ConnectedSpawnOptionHostNames.ReplyDemonitorAlias && !removeMonitor)
            await a.PutPacket(await b.TakePacket());
        else
            Require(b.Runtime.DistributionPending.ConnectedSnapshot(b.Handle!).QueuedPackets == 0);
        await caller.InvokeAsync(async context =>
        {
            if (mode != ConnectedSpawnOptionHostNames.ReplyDemonitorAlias && !removeMonitor)
            {
                var down = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                Require(down is TupleTerm { Items: [Atom { Name: ConnectedSpawnOptionHostNames.CustomDown }, var actual, _, var target, var reason] } &&
                    actual.Equals(reference) && target.Equals(child) && reason.Equals(Term.A(ConnectedSpawnHostNames.ChildReason)));
            }
            Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!.Equals(Term.A(ConnectedSpawnHostNames.Unrelated)));
            Term unaliased = await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnOptionHostNames.Unalias,
                reference
            );
            Require(unaliased.Equals(Term.A(mode == ConnectedSpawnOptionHostNames.ExplicitAlias ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)));
            Require(await context.ReceiveAsync(value => value, TimeSpan.Zero) is null);

            return true;
        });
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
