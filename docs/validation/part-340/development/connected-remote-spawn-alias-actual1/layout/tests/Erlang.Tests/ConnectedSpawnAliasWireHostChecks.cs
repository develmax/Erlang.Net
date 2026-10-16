namespace Erlang.Tests;

public static class ConnectedSpawnAliasWireHostChecks
{
    public static Task PlainSend2() => Exchange(false, false, false);

    public static Task TracedSend2() => Exchange(true, false, false);

    public static Task PlainSend3() => Exchange(false, true, false);

    public static Task AlternativeTracedSend3() => Exchange(true, true, true);

    private static async Task Exchange(bool traced, bool send3, bool alternative)
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        ulong flags = ConnectedSpawnHostNames.Flags | (alternative ? DistributionPassThroughFlags.AlternativeActionSignal : 0);
        await a.Connect(b, flags);
        await b.Connect(a, flags);
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

            return Nil.Value;
        }
        );
        var reference = (ReferenceTerm)await caller.InvokeAsync(async context =>
            await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SpawnRequest,
                Term.A(b.Runtime.Node),
                Term.A(ConnectedSpawnHostNames.TargetModule),
                Term.A(ConnectedSpawnHostNames.TargetFunction),
                Nil.Value,
                Term.List(Term.Tuple(
                    Term.A(ConnectedSpawnHostNames.Monitor),
                    Term.List(Term.Tuple(Term.A(ConnectedSpawnOptionHostNames.Alias), Term.A(ConnectedSpawnOptionHostNames.ReplyDemonitorAlias)))
                ))
            ));
        byte[] request = await a.TakePacket();
        var foreignAlias = (ReferenceTerm)DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: flags)
            .Control.Items[DistributionSpawnLayout.ReferenceIndex];
        await b.PutPacket(request);
        Pid child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        var childMonitor = (ReferenceTerm)await b.Controller.InvokeAsync(async context =>
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
            Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!.Equals(
                Term.Tuple(
                    Term.A(ConnectedSpawnHostNames.SpawnReply),
                    reference,
                    Term.A(ConnectedSpawnHostNames.Success),
                    child
                )));

            return true;
        });
        await b.Controller.InvokeAsync(async context =>
        {
            if (traced)
            {
                await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Send),
                    Term.A(ErlangBooleanAtoms.True)
                );
                await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.I(ConnectedSpawnOptionHostNames.Label)
                );
            }
            Term result = send3
                ? await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ErlangBasicBifNames.Send,
                    foreignAlias,
                    Term.A(ConnectedSpawnOptionHostNames.AliasMessage),
                    Term.List(Term.A(ConnectedSpawnOptionHostNames.NoConnect))
                )
                : await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ErlangBasicBifNames.Send,
                    foreignAlias,
                    Term.A(ConnectedSpawnOptionHostNames.AliasMessage)
                );
            Require(result.Equals(Term.A(send3 ? ConnectedSpawnHostNames.Success : ConnectedSpawnOptionHostNames.AliasMessage)));

            return true;
        });
        byte[] aliasPacket = await b.TakePacket();
        var decoded = DistributionPassThroughCodec.DecodeProcessPacket(aliasPacket, negotiatedFlags: flags);
        Require(decoded.Control.Items[0].Equals(Term.I(alternative ? DistributionControlOpcodes.AlternativeActionSend :
            traced ? DistributionControlOpcodes.AliasSendTrace : DistributionControlOpcodes.AliasSend)));
        Require(decoded.Control.Items[alternative ? DistributionAlternativePidSendLayout.TargetIndex : DistributionAliasSendLayout.TargetIndex].Equals(reference));
        Require(decoded.Payload!.Equals(Term.A(ConnectedSpawnOptionHostNames.AliasMessage)));
        await a.PutPacket(aliasPacket);
        await caller.InvokeAsync(async context =>
        {
            Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!.Equals(Term.A(ConnectedSpawnOptionHostNames.AliasMessage)));
            if (traced)
                Require((await a.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    SequenceTraceNames.Info,
                    Term.A(SequenceTraceNames.Label)
                )).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), Term.I(ConnectedSpawnOptionHostNames.Label))));

            return true;
        });
        byte[] demonitor = await a.TakePacket();
        Require(DistributionPassThroughCodec.DecodeProcessPacket(demonitor, negotiatedFlags: flags).Control.Items[0].Equals(Term.I(DistributionControlOpcodes.Demonitor)));
        await b.PutPacket(demonitor);
        b.Runtime.Send(child, Term.A(ConnectedSpawnHostNames.Release));
        await b.Controller.InvokeAsync(async context =>
        {
            Require(await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Down }, var actual, _, _, _] } tuple &&
                actual.Equals(childMonitor) ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            ) is not null);

            return true;
        });
        await a.PutPacket(aliasPacket);
        await caller.InvokeAsync(async context =>
        {
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
