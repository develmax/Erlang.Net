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
        await using var target = new RuntimeNodeFoundationHostActor(b.Runtime);
        Term label = new MapTerm([new(Term.A(ConnectedRpcHostNames.UserReason),
            Term.Tuple(new BitString([0, 255, 128]), Term.I(-123456789)))]);
        await target.InvokeAsync(async context => await b.Runtime.Modules.Call(
            context,
            ConnectedRpcHostNames.ProcLib,
            ConnectedRpcHostNames.SetLabel,
            label
        ));
        var foreign = new Pid(b.Runtime.Node, target.Process.Pid.Id, target.Process.Pid.Creation);

        Task<Term> BeginRead() => caller.InvokeAsync(async context => await a.Runtime.Modules.Call(
            context,
            ConnectedRpcHostNames.ProcLib,
            ConnectedRpcHostNames.GetLabel,
            foreign
        ));

        async Task<Term> CompleteRead(Task<Term> operation)
        {
            byte[] request = await a.TakePacket();
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: flags);
            Require(packet.Control.Items[DistributionSpawnLayout.MfaIndex].Equals(
                Term.Tuple(Term.A(ConnectedRpcHostNames.Erpc), Term.A(ConnectedRpcHostNames.Execute), Term.I(4))));
            Require(Cons.Items(packet.Payload!).Count() == 4);
            await b.PutPacket(request);
            await a.PutPacket(await b.TakePacket());
            await a.PutPacket(await b.TakePacket());

            return await operation.WaitAsync(ConnectedSpawnHostNames.Deadline);
        }
        Require((await CompleteRead(BeginRead())).Equals(label));
        await target.DisposeAsync();
        Require((await CompleteRead(BeginRead())).Equals(Term.A(ConnectedRpcHostNames.Undefined)));
        Task<Term> retired = BeginRead();
        _ = await a.TakePacket();
        await a.Reconnect(b);
        Require((await retired.WaitAsync(ConnectedSpawnHostNames.Deadline)).Equals(Term.A(ConnectedRpcHostNames.Undefined)));
        // Reconnect's default flags make this a genuinely hidden connection, absent from nodes().
        Require((await BeginRead().WaitAsync(ConnectedSpawnHostNames.Deadline)).Equals(Term.A(ConnectedRpcHostNames.Undefined)));
        Require(a.Runtime.DistributionPending.ConnectedSnapshot(a.Handle!).QueuedPackets == 0);
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
