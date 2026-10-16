namespace Erlang.Tests;

public static class ConnectedSpawnTraceHostChecks
{
    public static Task TracedSuccess() => Exchange(false);

    public static Task TracedCreatorError() => Exchange(true);

    private static async Task Exchange(bool badOption)
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        await using var tracerA = new RuntimeNodeFoundationHostActor(a.Runtime);
        await using var tracerB = new RuntimeNodeFoundationHostActor(b.Runtime);
        await tracerA.InvokeAsync(async context => await a.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            SequenceTraceNames.SystemFlag,
            Term.A(SequenceTraceNames.SystemTracer),
            context.Self
        ));
        await tracerB.InvokeAsync(async context => await b.Runtime.Modules.Call(
            context,
            ConnectedSpawnHostNames.Erlang,
            SequenceTraceNames.SystemFlag,
            Term.A(SequenceTraceNames.SystemTracer),
            context.Self
        ));
        var childToken = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        b.Runtime.Modules.Register(
            ConnectedSpawnHostNames.TargetModule,
            ConnectedSpawnHostNames.TargetFunction,
            0,
            async (context, _) =>
            {
                Term initial = await b.Runtime.Modules.Call(
                    context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.ProcessInfo,
                    context.Self, Term.A(ConnectedSpawnNativeHostNames.InitialCall)
                );
                Require(initial.Equals(Term.Tuple(Term.A(ConnectedSpawnNativeHostNames.InitialCall), Term.Tuple(
                    Term.A(ConnectedSpawnHostNames.TargetModule), Term.A(ConnectedSpawnHostNames.TargetFunction), Term.I(0)
                ))));
                Require((await b.Runtime.Modules.Call(
                    context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.ProcessInfo,
                    context.Self, Term.A(ConnectedSpawnHostNames.Links)
                )).Equals(Term.Tuple(Term.A(ConnectedSpawnHostNames.Links), Nil.Value)));
                childToken.TrySetResult(await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Info,
                    Term.A(SequenceTraceNames.Serial)
                ));

                return Nil.Value;
            }
        );
        var reference = (ReferenceTerm)await caller.InvokeAsync(async context =>
        {
            foreach (string flag in new[] { SequenceTraceNames.Send, SequenceTraceNames.Receive })
                await a.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Set,
                    Term.A(flag),
                    Term.A(ErlangBooleanAtoms.True)
                );
            await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                Term.I(ConnectedSpawnOptionHostNames.Label)
            );
            await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(0), Term.I(ConnectedSpawnOptionHostNames.InitialSerial))
            );

            return await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                badOption ? Term.List(Term.A(ConnectedSpawnOptionHostNames.InvalidOption)) : Nil.Value
            );
        });
        byte[] request = await a.TakePacket();
        var decodedRequest = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(decodedRequest.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnRequestTrace)));
        var requestToken = (TupleTerm)decodedRequest.Control.Items[DistributionSpawnLayout.RequestTokenIndex];
        Require(requestToken.Items[2].Equals(Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial)) && requestToken.Items[3].Equals(caller.Process.Pid));
        var beforeInput = b.Runtime.ProcessSnapshot().ToHashSet();
        await b.PutPacket(request);
        byte[] reply = await b.TakePacket();
        var decodedReply = DistributionPassThroughCodec.DecodeProcessPacket(reply, negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(decodedReply.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.SpawnReplyTrace)) && decodedReply.Payload is null);
        if (badOption)
        {
            Require(decodedReply.Control.Items[DistributionSpawnLayout.ReplyResultIndex].Equals(Term.A(ConnectedSpawnNativeHostNames.BadOption)));
            Require(!childToken.Task.IsCompleted && b.Runtime.ProcessSnapshot().ToHashSet().SetEquals(beforeInput));
        }
        var replyToken = (TupleTerm)decodedReply.Control.Items[DistributionSpawnLayout.ReplyTokenIndex];
        Require(replyToken.Items[2].Equals(Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial)) &&
            replyToken.Items[4].Equals(Term.I(ConnectedSpawnOptionHostNames.HeaderSerial)) &&
            replyToken.Items[3].Equals(badOption ? Term.A(b.Runtime.Node) : decodedReply.Control.Items[DistributionSpawnLayout.ReplyResultIndex]));
        await a.PutPacket(reply);
        await caller.InvokeAsync(async context =>
        {
            var response = (TupleTerm)(await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!;
            Require(response.Items[1].Equals(reference));
            Require(response.Items[2].Equals(Term.A(badOption ? ProcessSpawnRequestNames.Error : ConnectedSpawnHostNames.Success)));
            Term serial = await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                SequenceTraceNames.Info,
                Term.A(SequenceTraceNames.Serial)
            );
            Require(serial.Equals(Term.Tuple(
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(ConnectedSpawnOptionHostNames.HeaderSerial), Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial))
            )));

            return true;
        });
        if (!badOption)
            Require((await childToken.Task.WaitAsync(ConnectedSpawnHostNames.Deadline)).Equals(
                Term.Tuple(Term.A(SequenceTraceNames.Serial), Term.Tuple(Term.I(0), Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial)))));
        await tracerA.InvokeAsync(async context =>
        {
            var events = new List<TupleTerm>();
            for (int index = 0; index < 2; index++)
                events.Add((TupleTerm)(await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!);
            var first = (TupleTerm)events[0].Items[2];
            var second = (TupleTerm)events[1].Items[2];
            Require(first.Items[0].Equals(Term.A(SequenceTraceNames.Send)) && second.Items[0].Equals(Term.A(SequenceTraceNames.Send)));
            Require(first.Items[3].Equals(Term.A(b.Runtime.Node)) && second.Items[4] is Nil);
            Require(((TupleTerm)first.Items[1]).Items[1].Equals(Term.I(ConnectedSpawnOptionHostNames.HeaderSerial)) &&
                ((TupleTerm)second.Items[1]).Items[1].Equals(Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial)));

            return true;
        });
        await tracerB.InvokeAsync(async context =>
        {
            var events = new List<TupleTerm>();
            int expected = badOption ? 2 : 3;
            for (int index = 0; index < expected; index++)
                events.Add((TupleTerm)(await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!);
            var header = (TupleTerm)events[0].Items[2];
            var sentReply = (TupleTerm)events[1].Items[2];
            Require(header.Items[0].Equals(Term.A(SequenceTraceNames.Receive)) &&
                sentReply.Items[0].Equals(Term.A(SequenceTraceNames.Send)));
            Require(((TupleTerm)header.Items[1]).Items[1].Equals(Term.I(ConnectedSpawnOptionHostNames.HeaderSerial)));
            Require(((TupleTerm)sentReply.Items[1]).Items[1].Equals(Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial)));
            if (!badOption)
            {
                var arguments = (TupleTerm)events[2].Items[2];
                Require(arguments.Items[0].Equals(Term.A(SequenceTraceNames.Receive)) && arguments.Items[4] is Nil);
                Require(((TupleTerm)arguments.Items[1]).Items[1].Equals(Term.I(ConnectedSpawnOptionHostNames.ArgumentSerial)));
            }

            return true;
        });
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
