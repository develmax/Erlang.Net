using Erlang.Otp;

namespace Erlang.Tests;

public static class ConnectedProcLibNodeHostChecks
{
    public static Task SpawnFun() => Exchange(ConnectedProcLibNodeApi.SpawnFun);

    public static Task SpawnMfa() => Exchange(ConnectedProcLibNodeApi.SpawnMfa);

    public static Task SpawnLinkFun() => Exchange(ConnectedProcLibNodeApi.SpawnLinkFun);

    public static Task SpawnLinkMfa() => Exchange(ConnectedProcLibNodeApi.SpawnLinkMfa);

    public static Task SpawnOptFun() => Exchange(ConnectedProcLibNodeApi.SpawnOptFun);

    public static Task SpawnOptMfa() => Exchange(ConnectedProcLibNodeApi.SpawnOptMfa);

    private static async Task Exchange(ConnectedProcLibNodeApi api)
    {
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Runtime.InitializeModulesAsync(OtpModules.Register);
        await b.Runtime.InitializeModulesAsync(OtpModules.Register);
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        var entered = new TaskCompletionSource<TupleTerm>(TaskCreationOptions.RunContinuationsAsynchronously);
        int localInvocations = 0;
        a.Runtime.Modules.Register(
            ConnectedProcLibNodeHostNames.Module,
            ConnectedProcLibNodeHostNames.Child,
            0,
            (_, _) =>
        {
            Interlocked.Increment(ref localInvocations);
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
        }
        );
        b.Runtime.Modules.Register(
            ConnectedProcLibNodeHostNames.Module,
            ConnectedProcLibNodeHostNames.Child,
            0,
            async (context, _) =>
        {
            Term initial = await b.Runtime.Modules.Call(
                context,
                ConnectedRpcHostNames.ProcLib,
                ConnectedRpcHostNames.InitialCall,
                context.Self
            );
            Term ancestors = context.Dictionary[Term.A(ConnectedProcLibNodeHostNames.Ancestors)];
            Term leader = await b.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.GroupLeader);
            entered.TrySetResult(Term.Tuple(
                context.Self,
                initial,
                ancestors,
                leader
            ));
            await context.ReceiveAsync(value => value.Equals(Term.A(ConnectedSpawnHostNames.Release)) ? value : null);

            return Nil.Value;
        }
        );
        bool fun = api is ConnectedProcLibNodeApi.SpawnFun or ConnectedProcLibNodeApi.SpawnLinkFun or ConnectedProcLibNodeApi.SpawnOptFun;
        bool link = api is ConnectedProcLibNodeApi.SpawnLinkFun or ConnectedProcLibNodeApi.SpawnLinkMfa;
        bool monitor = api is ConnectedProcLibNodeApi.SpawnOptFun or ConnectedProcLibNodeApi.SpawnOptMfa;
        Task<Term> started = caller.InvokeAsync(async context =>
        {
            context.TrapExits = true;
            a.Runtime.Register(ConnectedProcLibNodeHostNames.Parent, context.Self);
            context.Dictionary[Term.A(ConnectedProcLibNodeHostNames.Ancestors)] = Term.List(Term.A(ConnectedProcLibNodeHostNames.Root));
            Term callback = fun ? await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedProcLibNodeHostNames.MakeFun,
                Term.A(ConnectedProcLibNodeHostNames.Module),
                Term.A(ConnectedProcLibNodeHostNames.Child),
                Term.I(0)
            ) : Nil.Value;
            string function = monitor ? ConnectedProcLibNodeHostNames.SpawnOpt : link ? ConnectedProcLibNodeHostNames.SpawnLink : ConnectedProcLibNodeHostNames.Spawn;
            Term[] arguments = fun
                ? monitor ? [Term.A(b.Runtime.Node), callback, Term.List(Term.A(ConnectedSpawnHostNames.Monitor))] : [Term.A(b.Runtime.Node), callback]
                : monitor ? [Term.A(b.Runtime.Node), Term.A(ConnectedProcLibNodeHostNames.Module), Term.A(ConnectedProcLibNodeHostNames.Child), Nil.Value, Term.List(Term.A(ConnectedSpawnHostNames.Monitor))]
                    : [Term.A(b.Runtime.Node), Term.A(ConnectedProcLibNodeHostNames.Module), Term.A(ConnectedProcLibNodeHostNames.Child), Nil.Value];

            return await a.Runtime.Modules.Call(
                context,
                ConnectedRpcHostNames.ProcLib,
                function,
                arguments
            );
        });
        bool completed = false;
        try
        {
            byte[] request = await a.TakePacket();
            ConnectedRpcHostTrace.Packet(api.ToString(), request);
            var packet = DistributionPassThroughCodec.DecodeProcessPacket(
                request,
                new EtfDecodeOptions
                {
                    ExternalFunctionFactory = (identity, safe) => b.Runtime.Functions.External(b.Runtime, identity, safe)
                },
                negotiatedFlags: ConnectedSpawnHostNames.Flags
            );
            ConnectedRpcHostTrace.State(a, packet.Control);
            Require(packet.Control.Items[DistributionSpawnLayout.MfaIndex].Equals(
                Term.Tuple(Term.A(ConnectedRpcHostNames.ProcLib), Term.A(ConnectedProcLibNodeHostNames.Initializer), Term.I(fun ? 3 : 5))));
            await b.PutPacket(request);
            TupleTerm snapshot = await entered.Task.WaitAsync(ConnectedSpawnHostNames.Deadline);
            var child = (Pid)snapshot.Items[0];
            Require(snapshot.Items[1].Equals(Term.Tuple(Term.A(ConnectedProcLibNodeHostNames.Module), Term.A(ConnectedProcLibNodeHostNames.Child), Nil.Value)));
            Require(snapshot.Items[2].Equals(Term.List(Term.A(ConnectedProcLibNodeHostNames.Parent), Term.A(ConnectedProcLibNodeHostNames.Root))));
            Require(snapshot.Items[3].Equals(packet.Control.Items[DistributionSpawnLayout.GroupLeaderIndex]));
            var localMonitor = (ReferenceTerm)await b.Controller.InvokeAsync(async context =>
                await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.Monitor,
                    Term.A(ProcessMonitorKinds.Process),
                    child
                ));
            await ConnectedRpcHostTrace.Forward(b, a);
            Term result = await started.WaitAsync(ConnectedSpawnHostNames.Deadline);
            Pid remote = monitor ? (Pid)((TupleTerm)result).Items[0] : (Pid)result;
            Require(remote.Equals(child) && remote.Node == b.Runtime.Node && localInvocations == 0);
            b.Runtime.Send(child, Term.A(ConnectedSpawnHostNames.Release));
            await b.Controller.InvokeAsync(async context =>
            {
                Require(await context.ReceiveAsync(
                    value => value is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Down }, var reference, _, _, _] } tuple &&
                    reference.Equals(localMonitor) ? tuple : null,
                    ConnectedSpawnHostNames.Deadline
                ) is not null);

                return true;
            });
            if (monitor || link)
                await ConnectedRpcHostTrace.Forward(b, a);
            await caller.InvokeAsync(async context =>
            {
                if (monitor)
                {
                    var pair = (TupleTerm)result;
                    var down = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                    Require(down is TupleTerm { Items: [Atom { Name: ConnectedSpawnHostNames.Down }, var reference, _, var target, Atom { Name: ProcessExitReasons.Normal }] } &&
                        reference.Equals(pair.Items[1]) && target.Equals(child));
                }
                else if (link)
                    Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!.Equals(
                        Term.Tuple(Term.A(ConnectedSpawnHostNames.Exit), child, Term.A(ProcessExitReasons.Normal))));

                return true;
            });
            completed = true;
        }
        finally
        {
            if (!completed)
                await ConnectedRpcHostTrace.CancelBlockedCaller(a, caller);
        }

    }

    private static void Require(bool passed)
    {
        if (!passed)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
