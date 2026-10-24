namespace Erlang.Tests;

internal static class SpawnMonitor431Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(SpawnMonitor431Names.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext c, string node) => c.Runtime.Modules.Call(
        c,
        SpawnMonitor431Names.Internal,
        SpawnMonitor431Names.Native,
        Term.A(node),
        Term.Tuple(Term.A(SpawnMonitor431Names.Target), Term.A(SpawnMonitor431Names.Run), Nil.Value),
        Term.List(Term.A(SpawnMonitor431Names.Monitor)),
        Term.A(SpawnMonitor431Names.Request)
    );

    private static ValueTask<Term?> Take(
        ProcessContext c,
        ReferenceTerm reference,
        string tag,
        TimeSpan timeout
    ) => c.Mailbox.ReceiveAsync<Term>(
        v => v is TupleTerm { Items.Count: >= 2 } t && t.Items[0].Equals(Term.A(tag)) && t.Items[1].Equals(reference) ? v : null,
        timeout,
        c.Cancellation
    );

    internal static async Task Lifetime()
    {
        foreach (ulong monitorFlags in new[] { 0UL, DistributionPassThroughFlags.ProcessMonitor, DistributionPassThroughFlags.NamedProcessMonitor })
        {
            await using var a = new ConnectedSpawnHostEndpoint();
            await using var b = new ConnectedSpawnHostEndpoint();
            await a.Initialize(ConnectedSpawnHostNames.NodeA);
            await b.Initialize(ConnectedSpawnHostNames.NodeB);
            await a.Connect(b, SpawnMonitor431Names.Flags | monitorFlags);
            await b.Connect(a, SpawnMonitor431Names.Flags | monitorFlags);
            RuntimeBootstrapDistSpawn430.Register(a.Runtime, a.Runtime.Modules);
            var entered = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
            b.Runtime.Modules.Register(
                SpawnMonitor431Names.Target,
                SpawnMonitor431Names.Run,
                0,
                async (c, _) =>
            {
                entered.TrySetResult(c.Self);
                await c.ReceiveAsync(v => v.Equals(Term.A(SpawnMonitor431Names.Release)) ? v : null);
                c.Exit(Term.A(SpawnMonitor431Names.Reason));

                return Nil.Value;
            }
            );
            await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
            ReferenceTerm reference = (ReferenceTerm)await caller.InvokeAsync(async c => await Call(c, b.Runtime.Node));
            byte[] request = await a.TakePacket();
            var decoded = DistributionPassThroughCodec.DecodeProcessPacket(request, negotiatedFlags: SpawnMonitor431Names.Flags | monitorFlags);
            Require(Cons.Items(decoded.Control.Items[5]).Any(v => v.Equals(Term.A(SpawnMonitor431Names.Monitor))));
            await b.PutPacket(request);
            Pid child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
            await a.PutPacket(await b.TakePacket());
            Term? reply = await caller.InvokeAsync(async c => await Take(
                c,
                reference,
                SpawnMonitor431Names.Reply,
                ConnectedSpawnHostNames.Deadline
            ));
            Require(reply is TupleTerm { Items.Count: 4 } r && r.Items[2].Equals(Term.A(SpawnMonitor431Names.Success)) && r.Items[3] is Pid);
            b.Runtime.Send(child, Term.A(SpawnMonitor431Names.Release));
            using (var deadline = new CancellationTokenSource(ConnectedSpawnHostNames.Deadline)) while (b.Runtime.IsAlive(child)) await Task.Delay(ConnectedSpawnHostNames.PollMilliseconds, deadline.Token);
            Require(b.Runtime.DistributionPending.ConnectedSnapshot(b.Handle!).QueuedPackets == 0);
            Require(await caller.InvokeAsync(async c => await Take(
                c,
                reference,
                SpawnMonitor431Names.Down,
                TimeSpan.Zero
            )) is null);
            a.Runtime.DistributionPending.CloseConnected(a.Handle!);
            Term? down = await caller.InvokeAsync(async c => await Take(
                c,
                reference,
                SpawnMonitor431Names.Down,
                ConnectedSpawnHostNames.Deadline
            ));
            Require(down is TupleTerm { Items.Count: 5 } d && d.Items[4].Equals(Term.A(SpawnMonitor431Names.NoConnection)));
        }
    }

    internal static async Task Demonitor()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b, SpawnMonitor431Names.Flags);
        await b.Connect(a, SpawnMonitor431Names.Flags);
        RuntimeBootstrapDistSpawn430.Register(a.Runtime, a.Runtime.Modules);
        var entered = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
        b.Runtime.Modules.Register(
            SpawnMonitor431Names.Target,
            SpawnMonitor431Names.Run,
            0,
            async (c, _) =>
        {
            entered.TrySetResult(c.Self);
            await c.ReceiveAsync(v => v.Equals(Term.A(SpawnMonitor431Names.Release)) ? v : null);

            return Nil.Value;
        }
        );
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        ReferenceTerm reference = (ReferenceTerm)await caller.InvokeAsync(async c => await Call(c, b.Runtime.Node));
        await b.PutPacket(await a.TakePacket());
        Pid child = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
        await a.PutPacket(await b.TakePacket());
        Require(await caller.InvokeAsync(async c => await Take(
            c,
            reference,
            SpawnMonitor431Names.Reply,
            ConnectedSpawnHostNames.Deadline
        )) is not null);
        Require((await caller.InvokeAsync(async c => await c.Runtime.Modules.Call(
            c,
            SpawnMonitor431Names.Erlang,
            SpawnMonitor431Names.Demonitor,
            reference
        ))).Equals(Term.A(SpawnMonitor431Names.True)));
        Require(a.Runtime.DistributionPending.ConnectedSnapshot(a.Handle!).QueuedPackets == 0);
        a.Runtime.DistributionPending.CloseConnected(a.Handle!);
        Require(await caller.InvokeAsync(async c => await Take(
            c,
            reference,
            SpawnMonitor431Names.Down,
            TimeSpan.Zero
        )) is null);
        b.Runtime.Send(child, Term.A(SpawnMonitor431Names.Release));
    }

    internal static async Task Abandon()
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b, SpawnMonitor431Names.Flags);
        await b.Connect(a, SpawnMonitor431Names.Flags);
        RuntimeBootstrapDistSpawn430.Register(a.Runtime, a.Runtime.Modules);
        b.Runtime.Modules.Register(
            SpawnMonitor431Names.Target,
            SpawnMonitor431Names.Run,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        ReferenceTerm reference = (ReferenceTerm)await caller.InvokeAsync(async c => await Call(c, b.Runtime.Node));
        Require((await caller.InvokeAsync(async c => await c.Runtime.Modules.Call(
            c,
            SpawnMonitor431Names.Erlang,
            SpawnMonitor431Names.Abandon,
            reference
        ))).Equals(Term.A(SpawnMonitor431Names.True)));
        await b.PutPacket(await a.TakePacket());
        await a.PutPacket(await b.TakePacket());
        Require(a.Runtime.DistributionPending.ConnectedSnapshot(a.Handle!).QueuedPackets == 0);
        Require(await caller.InvokeAsync(async c => await Take(
            c,
            reference,
            SpawnMonitor431Names.Reply,
            TimeSpan.Zero
        )) is null);
    }
}
