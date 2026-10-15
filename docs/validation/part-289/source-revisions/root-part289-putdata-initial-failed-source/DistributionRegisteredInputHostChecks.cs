using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class DistributionRegisteredInputHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        using var compiled = GeneratedModuleCompiler.Compile(DistributionRegisteredInputHostNames.Source);
        compiled.Definition.Register(runtime.Modules);
        Check(runtime.Modules.HasExport(ErlangModuleNames.Module, DistributionInputNames.Function, DistributionInputNames.Arity));
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);

            return await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionRegisteredInputHostNames.Local),
                Term.I(0)
            );
        });
        Term host = runtime.DistributionPending.BeginPending(Term.A(DistributionRegisteredInputHostNames.Remote));
        Term opaque = await controller.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeSetNodeHostNames.SetNode,
            Term.A(DistributionRegisteredInputHostNames.Remote),
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(DistributionRegisteredInputHostNames.RemoteCreation))
        ));
        await controller.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionControllerNames.InputHandler,
            opaque,
            input.Process.Pid
        ));
        runtime.Register(DistributionRegisteredInputHostNames.Sink, receiver.Process.Pid);
        var remote = new Pid(DistributionRegisteredInputHostNames.Remote, 1, DistributionRegisteredInputHostNames.RemoteCreation);
        Term payload = Term.A(DistributionRegisteredInputHostNames.Payload);
        byte[] send = DistributionPassThroughCodec.Encode(DistributionPassThroughCodec.Send(receiver.Process.Pid, payload));
        byte[] registered = DistributionPassThroughCodec.Encode(DistributionPassThroughCodec.SendRegistered(
            remote, Term.A(DistributionRegisteredInputHostNames.Sink), payload));
        Check((await Deliver(DistributionRegisteredInputHostNames.Direct, new BitString(send))).Equals(Term.A(CommonAtomNames.Ok)));
        await RequirePayload();
        Check((await Deliver(DistributionRegisteredInputHostNames.Dynamic, IoList(send))).Equals(Term.A(CommonAtomNames.Ok)));
        await RequirePayload();
        Check((await Deliver(DistributionRegisteredInputHostNames.Direct, IoList(registered))).Equals(Term.A(CommonAtomNames.Ok)));
        await RequirePayload();
        Check((await Deliver(DistributionRegisteredInputHostNames.Dynamic, new BitString(registered))).Equals(Term.A(CommonAtomNames.Ok)));
        await RequirePayload();
        Check(runtime.DistributionPending.ConnectedSnapshot(host).AcceptedInputPackets == 4);
        Check(runtime.DistributionPending.ConnectedSnapshot(host).RawInputAttempts == 4);
        Check((await Deliver(DistributionRegisteredInputHostNames.Direct, Nil.Value)).Equals(Term.A(CommonAtomNames.Ok)));
        Check((await Deliver(DistributionRegisteredInputHostNames.Dynamic, new BitString([]))).Equals(Term.A(CommonAtomNames.Ok)));
        Check(runtime.DistributionPending.ConnectedSnapshot(host).RawInputAttempts == 6);
        Term invalidList = Term.List(Term.A(DistributionRegisteredInputHostNames.Invalid));
        await RequireError(
            input,
            opaque,
            invalidList,
            ErlangErrorReasons.BadArgument
        );
        Check(runtime.DistributionPending.ConnectedSnapshot(host).RawInputAttempts == 6);
        await RequireError(
            input,
            Nil.Value,
            Nil.Value,
            ErlangErrorReasons.BadArgument
        );
        await RequireError(
            controller,
            opaque,
            Term.I(0),
            DistributionControllerNames.NotSupported
        );
        await RequireError(
            controller,
            opaque,
            invalidList,
            ErlangErrorReasons.BadArgument
        );
        await RequireError(
            controller,
            opaque,
            IoList(send),
            DistributionControllerNames.NotSupported
        );
        Check(runtime.DistributionPending.ConnectedSnapshot(host).RawInputAttempts == 6);
        await RequireError(
            input,
            opaque,
            Term.I(0),
            ErlangErrorReasons.BadArgument
        );
        await RequireError(
            input,
            opaque,
            new BitString([0], DistributionRegisteredInputHostNames.PartialBits),
            ErlangErrorReasons.BadArgument
        );
        Check(runtime.DistributionPending.ConnectedSnapshot(host).RawInputAttempts == 8);
        await RequireError(
            input,
            Nil.Value,
            invalidList,
            ErlangErrorReasons.BadArgument
        );
        await RequireError(
            input,
            Nil.Value,
            IoList(send),
            ErlangErrorReasons.BadArgument
        );
        Check(!controller.Process.Completion.IsCompleted && runtime.IsAlive(controller.Process.Pid));
        await Deliver(
            DistributionRegisteredInputHostNames.Direct,
            new BitString(DistributionPassThroughCodec.EncodeProcessControl(
            Term.Tuple(Term.I(DistributionControlOpcodes.Link), remote, receiver.Process.Pid)))
        );
        Term links = await receiver.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            context.Self,
            Term.A(ProcessInfoNames.Links)
        ));
        Check(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(remote))));
        runtime.DistributionPending.CloseConnected(host);

        Task<Term> Deliver(string function, Term data)
        {
            Term[] arguments = function == DistributionRegisteredInputHostNames.Dynamic
                ? [Term.A(ErlangModuleNames.Module), Term.A(DistributionInputNames.Function), opaque, data]
                : [opaque, data];

            return input.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                DistributionRegisteredInputHostNames.Module,
                function,
                arguments
            ));
        }

        async Task RequirePayload()
        {
            Term? received = await receiver.InvokeAsync(async context => await context.ReceiveAsync(
                value => value, DistributionRegisteredInputHostNames.Deadline));
            Check(received is not null && received.Equals(payload));
        }

        async Task RequireError(
            RuntimeNodeFoundationHostActor actor,
            Term handle,
            Term data,
            string reason
        )
        {
            Term captured = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                DistributionRegisteredInputHostNames.Module,
                DistributionRegisteredInputHostNames.Capture,
                handle,
                data
            ));
            Check(captured is TupleTerm { Items.Count: 3 });
            var fields = ((TupleTerm)captured).Items;
            Check(fields[0].Equals(Term.A(DistributionRegisteredInputHostNames.Error)) && fields[1].Equals(Term.A(reason)));
            Term frame = Cons.Items(fields[2]).First();
            Check(frame is TupleTerm { Items.Count: 4 });
            var stack = ((TupleTerm)frame).Items;
            Check(stack[0].Equals(Term.A(ErlangModuleNames.Module)) && stack[1].Equals(Term.A(DistributionInputNames.Function)));
            Check(stack[2].Equals(Term.List(handle, data)));
        }
    }

    private static Term IoList(byte[] packet) => new Cons(Term.I(packet[0]), new BitString(packet[1..]));

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionRegisteredInputHostNames.Failure);
    }
}
