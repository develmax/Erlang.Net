namespace Erlang.Tests;

public static class ConnectedSpawnCleanupHostChecks
{
    public static Task ParentDeathBeforeReply() => Cleanup(false);

    public static Task AbandonBeforeReply() => Cleanup(true);

    public static Task ParentDeathAfterReplyBeforeSelection() => Cleanup(false, true);

    private static async Task Cleanup(bool abandon, bool replyBeforeDeath = false)
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
            await context.ReceiveAsync(value => value);

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
                Term.List(Term.A(ConnectedSpawnHostNames.Link), Term.A(ConnectedSpawnHostNames.Monitor))
            ));
        byte[] request = await a.TakePacket();
        if (abandon)
        {
            Term accepted = await caller.InvokeAsync(async context =>
                await a.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.Abandon,
                    reference
                ));
            if (!accepted.Equals(Term.A(ErlangBooleanAtoms.True)))
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        }
        else if (!replyBeforeDeath)
        {
            await KillCaller();
        }

        async Task KillCaller()
        {
            await a.Controller.Invoke(context =>
            {
                a.Runtime.Exit(context, caller.Process.Pid, Term.A(ConnectedSpawnHostNames.Kill));

                return true;
            });
            Term reason = await caller.Process.Completion.WaitAsync(ConnectedSpawnHostNames.Deadline);
            if (!reason.Equals(Term.A(ConnectedSpawnHostNames.Killed)))
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        }
        await b.PutPacket(request);
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
        // No caller mailbox scan occurs between accepting the reply and its external death.
        if (replyBeforeDeath)
            await KillCaller();
        var cleanup = new List<DistributionDecodedProcessPacket>();
        for (int index = 0; index < 2; index++)
        {
            byte[] packet = await a.TakePacket();
            cleanup.Add(DistributionPassThroughCodec.DecodeProcessPacket(packet, negotiatedFlags: ConnectedSpawnHostNames.Flags));
            await b.PutPacket(packet);
        }
        if (!cleanup.Select(packet => (int)((Integer)packet.Control.Items[0]).Value).ToHashSet()
            .SetEquals([DistributionControlOpcodes.Demonitor, DistributionControlOpcodes.Exit]))
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        if (replyBeforeDeath && !cleanup[0].Control.Items[0].Equals(Term.I(DistributionControlOpcodes.Demonitor)))
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        string expectedReason = abandon ? ConnectedSpawnHostNames.Abandoned : ConnectedSpawnHostNames.Killed;
        if (!cleanup.Single(packet => packet.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.Exit)))
            .Control.Items[DistributionControlOpcodes.ExitReasonIndex].Equals(Term.A(expectedReason)))
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        await b.Controller.InvokeAsync(async context =>
        {
            var down = await context.ReceiveAsync(
                value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Down }, var monitor, _, var target, _] } tuple &&
                monitor.Equals(localMonitor) && target.Equals(child) ? tuple : null,
                ConnectedSpawnHostNames.Deadline
            );
            if (down is null || !down.Items[^1].Equals(Term.A(expectedReason)) || b.Runtime.IsAlive(child))
                throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
            if (b.Runtime.DistributionPending.TakeConnectedOutput(b.Handle!, context, out _) is { } unexpected)
            {
                byte[] raw = unexpected.Segments.SelectMany(segment => segment.ToArray()).ToArray();
                var packet = DistributionPassThroughCodec.DecodeProcessPacket(raw, negotiatedFlags: ConnectedSpawnHostNames.Flags);
                throw new InvalidOperationException(packet.Control.ToString());
            }

            return true;
        });
        if (abandon)
            await caller.InvokeAsync(async context =>
            {
                if (await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
                    throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);

                return true;
            });
    }
}
