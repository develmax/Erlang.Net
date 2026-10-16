namespace Erlang.Tests;

public static class ConnectedSpawnNativeAliasHostChecks
{
    public static async Task ThreeMonitorAliasModes()
    {
        foreach (string mode in new[]
        {
            ConnectedSpawnOptionHostNames.ExplicitAlias,
            ConnectedSpawnOptionHostNames.DemonitorAlias,
            ConnectedSpawnOptionHostNames.ReplyDemonitorAlias
        })
            await Exchange(mode);
    }

    private static async Task Exchange(string mode)
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        ProcessHandle child = a.Runtime.Spawn(async context =>
        {
            await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnHostNames.Release)) ? value : null);

            return Nil.Value;
        });
        ReferenceTerm cleanup = (ReferenceTerm)await caller.InvokeAsync(async context =>
            await a.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.Monitor,
                Term.A(ProcessMonitorKinds.Process), child.Pid));
        ReferenceTerm alias = (ReferenceTerm)await caller.InvokeAsync(async context =>
            await a.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.Monitor,
                Term.A(ProcessMonitorKinds.Process), child.Pid,
                Term.List(Term.Tuple(Term.A(ConnectedSpawnOptionHostNames.Alias), Term.A(mode)))));
        Require(!cleanup.Equals(alias));
        Require(await ChildMonitorCount(a, caller, child.Pid) == 2);
        await Deliver(a, b, alias, ConnectedSpawnNativeHostNames.First);
        await caller.InvokeAsync(async context =>
        {
            Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!
                .Equals(Term.A(ConnectedSpawnNativeHostNames.First)));

            return true;
        });
        bool replyMode = mode == ConnectedSpawnOptionHostNames.ReplyDemonitorAlias;
        Require(await ChildMonitorCount(a, caller, child.Pid) == (replyMode ? 1 : 2));
        await caller.InvokeAsync(async context =>
        {
            Term result = await a.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnOptionHostNames.Demonitor, alias,
                Term.List(Term.A(ConnectedSpawnOptionHostNames.Info)));
            Require(result.Equals(Term.A(replyMode ? ErlangBooleanAtoms.False : ErlangBooleanAtoms.True)));

            return true;
        });
        Require(await ChildMonitorCount(a, caller, child.Pid) == 1);
        await Deliver(a, b, alias, ConnectedSpawnNativeHostNames.Second);
        bool explicitMode = mode == ConnectedSpawnOptionHostNames.ExplicitAlias;
        await caller.InvokeAsync(async context =>
        {
            Term? message = await context.ReceiveAsync(value => value,
                explicitMode ? ConnectedSpawnHostNames.Deadline : TimeSpan.Zero);
            Require(explicitMode ? message is not null && message.Equals(Term.A(ConnectedSpawnNativeHostNames.Second)) : message is null);
            Term result = await a.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnOptionHostNames.Unalias, alias);
            Require(result.Equals(Term.A(explicitMode ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)));

            return true;
        });
        await Deliver(a, b, alias, ConnectedSpawnNativeHostNames.Third);
        await caller.InvokeAsync(async context =>
        {
            Require(await context.ReceiveAsync(value => value, TimeSpan.Zero) is null);

            return true;
        });
        a.Runtime.Send(child.Pid, Term.A(ConnectedSpawnHostNames.Release));
        await caller.InvokeAsync(async context =>
        {
            Term? down = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
            Require(down!.Equals(Term.Tuple(Term.A(ConnectedSpawnHostNames.Down), cleanup,
                Term.A(ProcessMonitorKinds.Process), child.Pid, Term.A(ProcessExitReasons.Normal))));
            Require(await context.ReceiveAsync(value => value, TimeSpan.Zero) is null);

            return true;
        });
        await child.Completion.WaitAsync(ConnectedSpawnHostNames.Deadline);
    }

    private static Task<int> ChildMonitorCount(ConnectedSpawnHostEndpoint endpoint, RuntimeNodeFoundationHostActor caller, Pid child)
        => caller.InvokeAsync(async context =>
        {
            var result = (TupleTerm)await endpoint.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.ProcessInfo, context.Self, Term.A(ConnectedSpawnNativeHostNames.Monitors));

            return Cons.Items(result.Items[1]).Count(value => value.Equals(Term.Tuple(Term.A(ProcessMonitorKinds.Process), child)));
        });

    private static async Task Deliver(ConnectedSpawnHostEndpoint target, ConnectedSpawnHostEndpoint sender, ReferenceTerm alias, string message)
    {
        Term payload = Term.A(message);
        await sender.Controller.InvokeAsync(async context =>
        {
            Require((await sender.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang,
                ErlangBasicBifNames.Send, alias, payload)).Equals(payload));

            return true;
        });
        byte[] bytes = await sender.TakePacket();
        var decoded = DistributionPassThroughCodec.DecodeProcessPacket(bytes, negotiatedFlags: ConnectedSpawnHostNames.Flags);
        Require(decoded.Control.Equals(Term.Tuple(Term.I(DistributionControlOpcodes.AliasSend), sender.Controller.Process.Pid, alias)));
        Require(decoded.Payload!.Equals(payload));
        await target.PutPacket(bytes);
    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
