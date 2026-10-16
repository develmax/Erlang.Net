using Erlang.Differential;
using Erlang.Otp;

namespace Erlang.Tests;

public static class RemoteSourceFunHostChecks
{
    public static Task SpawnCaptured() => Exchange(ConnectedProcLibNodeApi.SpawnFun);

    public static Task SpawnLinkCaptured() => Exchange(ConnectedProcLibNodeApi.SpawnLinkFun);

    public static Task SpawnOptCaptured() => Exchange(ConnectedProcLibNodeApi.SpawnOptFun);

    private static async Task Exchange(ConnectedProcLibNodeApi api)
    {
        using var sourceA = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.PayloadSource);
        using var sourceB = GeneratedModuleCompiler.Compile(RemoteSourceFunHostNames.PayloadSource);
        Require(!ReferenceEquals(sourceA.Definition, sourceB.Definition));
        Require(sourceA.CSharpSha256 == sourceB.CSharpSha256);
        await using var a = new ConnectedSpawnHostEndpoint();
        await using var b = new ConnectedSpawnHostEndpoint();
        await a.Runtime.InitializeModulesAsync(OtpModules.Register);
        await b.Runtime.InitializeModulesAsync(OtpModules.Register);
        sourceA.Definition.Register(a.Runtime.Modules);
        sourceB.Definition.Register(b.Runtime.Modules);
        await a.Initialize(ConnectedSpawnHostNames.NodeA);
        await b.Initialize(ConnectedSpawnHostNames.NodeB);
        await a.Connect(b);
        await b.Connect(a);
        await using var caller = new RuntimeNodeFoundationHostActor(a.Runtime);
        bool link = api == ConnectedProcLibNodeApi.SpawnLinkFun;
        bool monitor = api == ConnectedProcLibNodeApi.SpawnOptFun;
        Term marker = Term.A(RemoteSourceFunHostNames.MarkerName);
        Require(b.Runtime.WhereIs(RemoteSourceFunHostNames.MarkerName).Equals(Term.A(RemoteSourceFunHostNames.Undefined)));
        MapTerm? capture = null;
        Atom? anonymousName = null;
        Term? originGroupLeader = null;
        Task<Term> started = caller.InvokeAsync(async context =>
        {
            context.TrapExits = true;
            originGroupLeader = await a.Runtime.Modules.Call(context, ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.GroupLeader);
            context.Dictionary[Term.A(RemoteSourceFunHostNames.Ancestors)] = Term.List(Term.A(RemoteSourceFunHostNames.Root));
            capture = new MapTerm([
                new(Term.A(RemoteSourceFunHostNames.Parent), context.Self),
                new(Term.A(RemoteSourceFunHostNames.Marker), marker),
                new(Term.A(RemoteSourceFunHostNames.Label), Term.Tuple(marker, Term.I(17))),
                new(Term.A(RemoteSourceFunHostNames.Payload), Term.Tuple(new BitString(new byte[] { 0, 128, 255 }), Term.List(Term.I(1), Term.I(2))))
            ]);
            var callback = (FunctionTerm)await a.Runtime.Modules.Call(
                context,
                RemoteSourceFunHostNames.Module,
                RemoteSourceFunHostNames.Maker,
                capture
            );
            Require(callback.LocalIdentity is not null && callback.ExternalIdentity is null && callback.Environment.Count > 0);
            anonymousName = callback.LocalIdentity!.Name;
            Term[] arguments = monitor ? [Term.A(b.Runtime.Node), callback, Term.List(Term.A(ConnectedSpawnHostNames.Monitor))]
                : [Term.A(b.Runtime.Node), callback];

            return await a.Runtime.Modules.Call(
                context,
                ConnectedRpcHostNames.ProcLib,
                monitor ? ConnectedProcLibNodeHostNames.SpawnOpt : link ? ConnectedProcLibNodeHostNames.SpawnLink : ConnectedProcLibNodeHostNames.Spawn,
                arguments
            );
        });
        bool completed = false;
        try
        {
            await b.PutPacket(await a.TakePacket());
            await a.PutPacket(await b.TakePacket());
            Term result = await started.WaitAsync(ConnectedSpawnHostNames.Deadline);
            Pid child = monitor ? (Pid)((TupleTerm)result).Items[0] : (Pid)result;
            Require(child.Node == b.Runtime.Node);
            await a.PutPacket(await b.TakePacket());
            await caller.InvokeAsync(async context =>
            {
                var ready = (TupleTerm)(await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!;
                Require(ready.Items[0].Equals(Term.A(RemoteSourceFunHostNames.RemoteReady)) && ready.Items[1].Equals(child));
                var snapshot = (MapTerm)ready.Items[2];
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.SnapshotNode)).Equals(Term.A(b.Runtime.Node)));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.GroupLeader)).Equals(originGroupLeader));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.Links)).Equals(Term.Tuple(
                    Term.A(RemoteSourceFunHostNames.Links), link ? Term.List(context.Self) : Nil.Value
                )));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.Captured)).Equals(capture));
                Require(anonymousName is not null);
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.InitialCall)).Equals(
                    Term.Tuple(Term.A(RemoteSourceFunHostNames.Module), anonymousName!, Nil.Value)));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.RawInitialCall)).Equals(
                    Term.Tuple(Term.A(RemoteSourceFunHostNames.Module), anonymousName!, Term.I(0))));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.ConstructorMarker)).Equals(Term.A(RemoteSourceFunHostNames.Undefined)));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.SnapshotAncestors)).Equals(Term.List(marker, Term.A(RemoteSourceFunHostNames.Root))));
                Require(snapshot.Get(Term.A(RemoteSourceFunHostNames.Label)).Equals(capture!.Get(Term.A(RemoteSourceFunHostNames.Label))));

                return true;
            });
            Require(b.Runtime.WhereIs(RemoteSourceFunHostNames.MarkerName).Equals(Term.A(RemoteSourceFunHostNames.Undefined)));
            await b.Controller.InvokeAsync(async context =>
            {
                Term translated = await b.Runtime.Modules.Call(
                    context,
                    ConnectedRpcHostNames.ProcLib,
                    RemoteSourceFunHostNames.TranslateInitialCall,
                    child
                );
                Require(translated.Equals(Term.Tuple(Term.A(RemoteSourceFunHostNames.Module), anonymousName!, Term.I(0))));

                return true;
            });
            ReferenceTerm cleanup = (ReferenceTerm)await b.Controller.InvokeAsync(async context =>
                await b.Runtime.Modules.Call(
                    context,
                    ConnectedSpawnHostNames.Erlang,
                    ConnectedSpawnHostNames.Monitor,
                    Term.A(ProcessMonitorKinds.Process),
                    child
                ));
            await caller.InvokeAsync(async context => await a.Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ErlangBasicBifNames.Send,
                child,
                Term.Tuple(Term.A(RemoteSourceFunHostNames.Finish), context.Self)
            ));
            await b.PutPacket(await a.TakePacket());
            await b.Controller.InvokeAsync(async context =>
            {
                Require((await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline))!.Equals(
                    Term.Tuple(
                        Term.A(ConnectedSpawnHostNames.Down),
                        cleanup,
                        Term.A(ProcessMonitorKinds.Process),
                        child,
                        Term.A(ProcessExitReasons.Normal)
                    )));

                return true;
            });
            if (monitor || link)
            {
                await a.PutPacket(await b.TakePacket());
                await caller.InvokeAsync(async context =>
                {
                    Term? ended = await context.ReceiveAsync(value => value, ConnectedSpawnHostNames.Deadline);
                    Term expected = monitor ? Term.Tuple(
                        Term.A(ConnectedSpawnHostNames.Down),
                        ((TupleTerm)result).Items[1],
                        Term.A(ProcessMonitorKinds.Process),
                        child,
                        Term.A(ProcessExitReasons.Normal)
                    )
                        : Term.Tuple(Term.A(ConnectedSpawnHostNames.Exit), child, Term.A(ProcessExitReasons.Normal));
                    Require(ended!.Equals(expected));

                    return true;
                });
            }
            completed = true;
        }
        finally
        {
            if (!completed)
                await ConnectedRpcHostTrace.CancelBlockedCaller(a, caller);
        }
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(ConnectedSpawnHostNames.Failure);
    }
}
