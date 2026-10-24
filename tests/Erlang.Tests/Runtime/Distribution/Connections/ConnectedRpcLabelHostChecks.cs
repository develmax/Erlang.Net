using Erlang.Otp;

namespace Erlang.Tests;

public static class ConnectedRpcLabelHostChecks
{
    public static async Task VisibleLabelDeathAndNodeRetirement()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Runtime.InitializeModulesAsync(OtpModules.Register);
        await b.Runtime.InitializeModulesAsync(OtpModules.Register);
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        ulong flags = ConnectedSpawnHostNames.Flags | ConnectedRpcHostNames.Published;
        await a.Connect(b, flags);
        await b.Connect(a, flags);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        Term label = new MapTerm([new(Term.A(ConnectedRpcHostNames.UserReason),
            Term.Tuple(new BitString([0, 255, 128]), Term.I(-123456789)))]);
        var targetReady = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = b.Runtime.Spawn(async context =>
        {
            await b.Runtime.Modules.Call(
                context,
                ConnectedRpcHostNames.ProcLib,
                ConnectedRpcHostNames.SetLabel,
                label
            );
            targetReady.TrySetResult(context.Self);
            await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnHostNames.Release)) ? value : null);

            return Nil.Value;
        });
        Pid targetPid = await targetReady.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        var foreign = new Pid(b.Runtime.Node, targetPid.Id, targetPid.Creation);

        Task<Term> BeginRead() => caller.InvokeAsync(async context => await a.Runtime.Modules.Call(
            context,
            ConnectedRpcHostNames.ProcLib,
            ConnectedRpcHostNames.GetLabel,
            foreign
        ));

        async Task<Term> CompleteRead(Task<Term> operation)
        {
            byte[] request = await a.TakePacket();
            ConnectedRpcHostTrace.Packet(ConnectedSpawnHostNames.NodeA, request);
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: flags);
            ConnectedRpcHostTrace.State(a, packet.Control);
            Require(packet.Control.Items[DistributionSpawnLayout.MfaIndex].Equals(
                Term.Tuple(Term.A(ConnectedRpcHostNames.Erpc), Term.A(ConnectedRpcHostNames.Execute), Term.I(4))));
            Require(Cons.Items(packet.Payload!).Count() == 4);
            await b.PutPacket(request);
            await ConnectedRpcHostTrace.Forward(b, a);
            await ConnectedRpcHostTrace.Forward(b, a);

            return await operation.WaitAsync(ConnectedSpawnHostNames.Deadline);
        }
        bool completed = false;
        try
        {
            Term visible = await caller.InvokeAsync(async context => await a.Runtime.Modules.Call(context,
                ConnectedSpawnHostNames.Erlang, ConnectedRpcHostNames.Nodes));
            Console.WriteLine(visible);
            Require(Cons.Items(visible).Any(node => node.Equals(b.Runtime.NodeAtom)));
            Require((await CompleteRead(BeginRead())).Equals(label));
            b.Runtime.Send(target.Pid, Term.A(ConnectedSpawnHostNames.Release));
            await target.Completion.WaitAsync(ConnectedSpawnHostNames.Deadline);
            Require((await CompleteRead(BeginRead())).Equals(Term.A(ConnectedRpcHostNames.Undefined)));
            Task<Term> retired = BeginRead();
            _ = await a.TakePacket();
            await a.Reconnect(b);
            Require((await retired.WaitAsync(ConnectedSpawnHostNames.Deadline)).Equals(Term.A(ConnectedRpcHostNames.Undefined)));
            // Reconnect's default flags make this a genuinely hidden connection, absent from nodes().
            Require((await BeginRead().WaitAsync(ConnectedSpawnHostNames.Deadline)).Equals(Term.A(ConnectedRpcHostNames.Undefined)));
            Require(a.Runtime.DistributionPending.ConnectedSnapshot(a.Handle!).QueuedPackets == 0);
            completed = true;
        }
        finally
        {
            if (!completed)
                await ConnectedRpcHostTrace.CancelBlockedCaller(a, caller);
            b.Runtime.Send(target.Pid, Term.A(ConnectedSpawnHostNames.Release));
            await target.Completion.WaitAsync(ConnectedSpawnHostNames.Deadline);
        }

    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
