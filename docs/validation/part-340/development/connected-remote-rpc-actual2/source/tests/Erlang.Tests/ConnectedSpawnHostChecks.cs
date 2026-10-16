namespace Erlang.Tests;

public static class ConnectedSpawnHostChecks
{
    public static Task CodecFrames()
    {
        var parent = new Pid(ConnectedSpawnHostNames.NodeA, 1);
        var reference = new ReferenceTerm(ConnectedSpawnHostNames.NodeA, 1);
        var mfa = Term.Tuple(Term.A(ConnectedSpawnHostNames.TargetModule), Term.A(ConnectedSpawnHostNames.TargetFunction), Term.I(0));
        var control = Term.Tuple(
            Term.I(DistributionControlOpcodes.SpawnRequest),
            reference,
            parent,
            parent,
            mfa,
            Nil.Value
        );
        byte[] encoded = DistributionPassThroughCodec.Encode(new(control, Nil.Value), negotiatedFlags: ConnectedSpawnHostNames.Flags);
        var decoded = DistributionPassThroughCodec.DecodeProcessPacket(encoded, negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(decoded.Control.Equals(control) && decoded.Payload is Nil);
        Reject<NotSupportedException>(() => DistributionPassThroughCodec.Encode(new(control, Nil.Value)));
        Reject<ArgumentException>(() => DistributionPassThroughCodec.Encode(new(control, Term.List(Term.I(1))), negotiatedFlags: ConnectedSpawnHostNames.Flags));
        var improper = Term.Tuple(
            Term.I(DistributionControlOpcodes.SpawnRequest),
            reference,
            parent,
            parent,
            mfa,
            new Cons(Term.A(ConnectedSpawnHostNames.Link), Term.I(0))
        );
        Reject<ArgumentException>(() => DistributionPassThroughCodec.Encode(new(improper, Nil.Value), negotiatedFlags: ConnectedSpawnHostNames.Flags));
        var traced = Term.Tuple(
            Term.I(DistributionControlOpcodes.SpawnRequestTrace),
            reference,
            parent,
            parent,
            mfa,
            Nil.Value,
            Nil.Value
        );
        var tracedDecoded = DistributionPassThroughCodec.DecodeProcessPacket(
            DistributionPassThroughCodec.Encode(new(traced, Nil.Value), negotiatedFlags: ConnectedSpawnHostNames.Flags),
            negotiatedFlags: ConnectedSpawnHostNames.Flags
        );
        Require(tracedDecoded.Control.Equals(traced));
        foreach (int opcode in new[] { DistributionControlOpcodes.SpawnReply, DistributionControlOpcodes.SpawnReplyTrace })
        {
            var reply = opcode == DistributionControlOpcodes.SpawnReply
                ? Term.Tuple(
                    Term.I(opcode),
                    reference,
                    parent,
                    Term.I(0),
                    Term.A(ConnectedSpawnHostNames.ChildReason)
                )
                : Term.Tuple(
                    Term.I(opcode),
                    reference,
                    parent,
                    Term.I(0),
                    Term.A(ConnectedSpawnHostNames.ChildReason),
                    Nil.Value
                );
            byte[] packet = DistributionPassThroughCodec.EncodeProcessControl(reply, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            var parsed = DistributionPassThroughCodec.DecodeProcessPacket(packet, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(parsed.Payload is null && parsed.Control.Equals(reply));
            Reject<ArgumentException>(() => DistributionPassThroughCodec.DecodeProcessPacket(packet.Concat(ExternalTermFormat.Encode(Nil.Value)).ToArray(), negotiatedFlags: ConnectedSpawnHostNames.Flags));
        }
        var invalidFlags = Term.Tuple(
            Term.I(DistributionControlOpcodes.SpawnReply),
            reference,
            parent,
            Term.I((long)DistributionSpawnLayout.MaximumReplyFlags + 1),
            Term.A(ConnectedSpawnHostNames.ChildReason)
        );
        Reject<ArgumentException>(() => DistributionPassThroughCodec.EncodeProcessControl(invalidFlags, negotiatedFlags: ConnectedSpawnHostNames.Flags));

        return Task.CompletedTask;
    }

    public static async Task RealRequestReplyAndDeath()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var entered = new TaskCompletionSource<TupleTerm>(TaskCreationOptions.RunContinuationsAsynchronously);
        b.Runtime.Modules.Register(
            ConnectedSpawnHostNames.TargetModule,
            ConnectedSpawnHostNames.TargetFunction,
            1,
            async (context, arguments) =>
        {
            Term leader = await b.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.GroupLeader);
            Term links = await b.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.ProcessInfo,
                context.Self,
                Term.A(ConnectedSpawnHostNames.Links)
            );
            entered.TrySetResult(Term.Tuple(context.Self, leader, links));
            await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnHostNames.Release)) ? value : null);
            context.Exit(arguments[0]);

            return Nil.Value;
        }
        );
        try
        {
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
                    Term.List(Term.A(ConnectedSpawnHostNames.ChildReason)),
                    Term.List(Term.A(ConnectedSpawnHostNames.Link), Term.A(ConnectedSpawnHostNames.Monitor))
                );
            });
            byte[] request = await a.TakePacket();
            var requestPacket = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(requestPacket.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnRequest)));
            Require(requestPacket.Control.Items[DistributionSpawnLayout.ReferenceIndex].Equals(reference));
            Require(requestPacket.Payload!.Equals(Term.List(Term.A(ConnectedSpawnHostNames.ChildReason))));
            Require(!entered.Task.IsCompleted);
            await b.PutPacket(request);
            TupleTerm observed = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
            var child = (Pid)observed.Items[0];
            Require(child.Node == b.Runtime.Node && child.Node != a.Runtime.Node);
            Require(observed.Items[1].Equals(requestPacket.Control.Items[DistributionSpawnLayout.GroupLeaderIndex]));
            Require(observed.Items[2] is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Links }, var links] } && Cons.Items(links).Contains(caller.Process.Pid));
            byte[] reply = await b.TakePacket();
            var replyPacket = DistributionPassThroughCodec.DecodeProcessPacket(reply, negotiatedFlags: ConnectedSpawnHostNames.Flags);
            Require(replyPacket.Payload is null);
            Require(replyPacket.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnReply)));
            Require(replyPacket.Control.Items[DistributionSpawnLayout.ReplyResultIndex].Equals(child));
            Require(replyPacket.Control.Items[DistributionSpawnLayout.ReplyFlagsIndex].Equals(Term.I(DistributionSpawnLayout.LinkFlag | DistributionSpawnLayout.MonitorFlag)));
            await a.PutPacket(reply);
            b.Runtime.Send(child, Term.A(ConnectedSpawnHostNames.Release));
            // The caller has not scanned its mailbox: real EXIT/DOWN must still correlate after an accepted reply.
            var deathPackets = new List<DistributionDecodedProcessPacket>();
            for (int index = 0; index < 2; index++)
            {
                byte[] packet = await b.TakePacket();
                deathPackets.Add(DistributionPassThroughCodec.DecodeProcessPacket(packet, negotiatedFlags: ConnectedSpawnHostNames.Flags));
                await a.PutPacket(packet);
            }
            Require(deathPackets.Select(packet => (int)((Integer)packet.Control.Items[0]).Value).ToHashSet()
                .SetEquals([DistributionControlOpcodes.Exit, DistributionControlOpcodes.MonitorExit]));
            await caller.InvokeAsync(async context =>
            {
                Term? first = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                Term? second = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                Require(first!.Equals(Term.A(ConnectedSpawnHostNames.Unrelated)));
                Require(second is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.SpawnReply }, var correlation, Atom { Name: ConnectedSpawnHostNames.Success }, var spawned] } &&
                    correlation.Equals(reference) && spawned.Equals(child));
                var exit = await context.ReceiveAsync(
                    value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Exit }, var source, _] } tuple && source.Equals(child) ? tuple : null,
                    ConnectedSpawnHostNames.Deadline
                );
                var down = await context.ReceiveAsync(
                    value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Down }, var correlation, _, var source, _] } tuple &&
                    correlation.Equals(reference) && source.Equals(child) ? tuple : null,
                    ConnectedSpawnHostNames.Deadline
                );
                Require(exit is not null && down is not null && exit.Items[^1].Equals(Term.A(ConnectedSpawnHostNames.ChildReason)) && down.Items[^1].Equals(Term.A(ConnectedSpawnHostNames.ChildReason)));

                return true;
            });
        }
        finally
        {
            if (entered.Task.IsCompletedSuccessfully)
                b.Runtime.Send((Pid)entered.Task.Result.Items[0], Term.A(ConnectedSpawnHostNames.Release));
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
