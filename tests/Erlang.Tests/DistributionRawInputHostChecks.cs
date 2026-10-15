using Erlang;

namespace Erlang.Tests;

internal static class DistributionRawInputHostChecks
{
    internal static async Task Run()
    {
        await RunCase(false);
        await RunCase(true);
    }

    private static async Task RunCase(bool localKernelStop)
    {
        int checks = 0;
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var replacement = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            _ = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionRawInputHostNames.Local),
                Term.I(0)
            );

            return true;
        });
        var pending = runtime.DistributionPending;
        Term host = pending.BeginPending(Term.A(DistributionRawInputHostNames.Remote));
        Term opaque = await Adopt(host);
        await Failure(ErlangErrorReasons.BadArgument, Nil.Value, Nil.Value);
        await Failure(ErlangErrorReasons.BadArgument, Nil.Value,
            new Cons(Term.A(DistributionRawInputHostNames.Payload), Nil.Value));
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 0);
        await controller.Invoke(context =>
        {
            Expect(DistributionControllerNames.NotSupported,
                () => DistributionInputAdapter.PutData(context, opaque, Nil.Value));

            return true;
        });
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 0);
        Check((await Put(Nil.Value)).Equals(DistributionRawInputHostNames.Ok));
        Check((await Put(new BitString([]))).Equals(DistributionRawInputHostNames.Ok));
        await Failure(ErlangErrorReasons.BadArgument, opaque, Term.I(0));
        await Failure(ErlangErrorReasons.BadArgument, opaque,
            new BitString(new byte[] { 0 }, DistributionRawInputHostNames.PartialBits));
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 4);
        Check(!controller.Process.Completion.IsCompleted);
        var payload = Term.A(DistributionRawInputHostNames.Payload);
        var packet = DistributionPassThroughCodec.Encode(
            DistributionPassThroughCodec.Send(receiver.Process.Pid, payload));
        Check((await Put(new BitString(packet))).Equals(DistributionRawInputHostNames.Ok));
        Check((await receiver.InvokeAsync(async context =>
            await context.ReceiveAsync(message => message, DistributionRawInputHostNames.Deadline)))!.Equals(payload));
        Check(pending.ConnectedSnapshot(host).AcceptedInputPackets == 1);
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 5);
        var remote = new Pid(DistributionRawInputHostNames.Remote, 1, 0);
        var reference = new ReferenceTerm(DistributionRawInputHostNames.Remote, new uint[] { 1 }, 0);
        await Unsupported(Term.Tuple(
            Term.I(DistributionRawInputHostNames.GroupLeaderOpcode),
            remote,
            receiver.Process.Pid
        ));
        Check(!controller.Process.Completion.IsCompleted);
        Check((await Put(Cons.From(packet.Select(value => (Term)Term.I(value))))).Equals(DistributionRawInputHostNames.Ok));
        Check((await receiver.InvokeAsync(async context =>
            await context.ReceiveAsync(message => message, DistributionRawInputHostNames.Deadline)))!.Equals(payload));
        Check(pending.ConnectedSnapshot(host).AcceptedInputPackets == 2);
        var controllerContext = await controller.Ready;
        var identity = new LocalFunctionIdentity(
            Term.A(ConnectedIngressHostNames.CookieModule),
            null,
            ConnectedIngressHostNames.FunctionIndex,
            ConnectedIngressHostNames.FunctionOldUnique,
            new BitString(new byte[ConnectedIngressHostNames.UniqueBytes])
        );
        bool race = false;
        _ = runtime.Functions.Register(
            identity,
            ConnectedIngressHostNames.FunctionArity,
            ConnectedIngressHostNames.FunctionEnvironment,
            environment =>
        {
            if (race)
            {
                pending.CloseConnected(host);
                host = pending.BeginPending(Term.A(DistributionRawInputHostNames.Remote));
                opaque = DistributionControllerAdapters.SetNode(
                    controllerContext,
                    Term.A(DistributionRawInputHostNames.Remote),
                    controllerContext.Self,
                    Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(0))
                ).GetAwaiter().GetResult();
                _ = DistributionControllerAdapters.SetInputHandler(controllerContext, opaque, input.Process.Pid);
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }

            return new FunctionTerm(
                identity,
                ConnectedIngressHostNames.FunctionArity,
                environment,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
        }
        );
        var cookie = runtime.Functions.Restore(
            runtime,
            identity,
            ConnectedIngressHostNames.FunctionArity,
            []
        );
        byte[] racingPacket = DistributionPassThroughCodec.Encode(
            new(
            Term.Tuple(Term.I(DistributionPassThroughLayout.SendOpcode), cookie, receiver.Process.Pid), payload),
            options: new EtfEncodingOptions { FunctionOwner = controller.Process.Pid }
        );
        race = true;
        Check((await Put(new BitString(racingPacket))).Equals(DistributionRawInputHostNames.Ok));
        race = false;
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 0);
        Check(pending.ConnectedSnapshot(host).AcceptedInputPackets == 0);
        Check(!controller.Process.Completion.IsCompleted);
        Term oldOpaque = opaque;
        pending.CloseConnected(host);
        host = pending.BeginPending(Term.A(DistributionRawInputHostNames.Remote));
        opaque = await Adopt(host);
        Check((await input.Invoke(context => DistributionInputAdapter.PutData(context,
            oldOpaque, new BitString(new byte[] { 0 })))).Equals(DistributionRawInputHostNames.Ok));
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 1);
        Check(pending.ConnectedSnapshot(host).AcceptedInputPackets == 0);
        Check(!controller.Process.Completion.IsCompleted);
        using var state = pending.Acquire(host);
        Check((await Put(new BitString(new byte[] { 0 }))).Equals(DistributionRawInputHostNames.Ok));
        Check(state.Snapshot().State == DistributionPendingState.Exiting);
        Check(!controller.Process.Completion.IsCompleted);
        Check(DoomedControllers() == 1);
        if (localKernelStop)
        {
            typeof(ProcessRuntime).GetMethod(
                DistributionRawInputHostNames.StopLocalNode,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null
            )!.Invoke(runtime, null);
            await kernel.InvokeAsync(async context =>
            {
                _ = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ConnectedIngressHostNames.SetNode,
                    Term.A(DistributionRawInputHostNames.Local),
                    Term.I(0)
                );

                return true;
            });
        }
        else
        {
            await kernel.Invoke(context =>
            {
                pending.ReserveKernelSetup(context);
                pending.ReleaseKernelSetup(context);

                return true;
            });
        }
        Check(DoomedControllers() == 1);
        Check(state.Snapshot().State == DistributionPendingState.Idle);
        host = pending.BeginPending(Term.A(DistributionRawInputHostNames.Remote));
        await controller.Invoke(context =>
        {
            Expect(
                ErlangErrorReasons.BadArgument,
                () =>
                DistributionControllerAdapters.SetNode(
                    context,
                    Term.A(DistributionRawInputHostNames.Remote),
                    context.Self,
                    Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(0))
                ).GetAwaiter().GetResult()
            );

            return true;
        });
        opaque = await Adopt(host, replacement);
        Check(pending.ConnectedSnapshot(host).Controller.Equals(replacement.Process.Pid));
        _ = controller.InvokeAsync(async context =>
        {
            await context.ReceiveAsync(message => message, DistributionRawInputHostNames.Deadline);

            return true;
        });
        var reason = await controller.Process.Completion.WaitAsync(DistributionRawInputHostNames.Deadline);
        Check(reason.Equals(Term.A(ProcessExitReasons.Killed)));
        Check(DoomedControllers() == 0);
        Check(!replacement.Process.Completion.IsCompleted);
        Check(pending.ConnectedSnapshot(host).Controller.Equals(replacement.Process.Pid));
        Check(pending.ConnectedSnapshot(host).RawInputAttempts == 0);
        if (checks != DistributionRawInputHostNames.ExpectedAssertions)
            throw new InvalidOperationException(DistributionRawInputHostNames.Failure);

        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            RawInputAssertions = checks,
            LocalKernelStop = localKernelStop,
            NativeExecuted = false
        }));

        async Task<Term> Adopt(Term handle, RuntimeNodeFoundationHostActor? owner = null)
        {
            owner ??= controller;
            var result = await owner.InvokeAsync(async context =>
                await DistributionControllerAdapters.SetNode(
                    context,
                    Term.A(DistributionRawInputHostNames.Remote),
                    context.Self,
                    Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(0))
                ));
            await owner.Invoke(context =>
                DistributionControllerAdapters.SetInputHandler(context, result, input.Process.Pid));

            return result;
        }

        Task<Term> Put(Term data) => input.Invoke(context =>
            DistributionInputAdapter.PutData(context, opaque, data));

        async Task Failure(string reason, Term handle, Term data)
        {
            await input.Invoke(context =>
            {
                Expect(reason, () => DistributionInputAdapter.PutData(context, handle, data));

                return true;
            });
        }

        async Task Unsupported(TupleTerm control)
        {
            byte[] bytes = ExternalTermFormat.Encode(control);
            byte[] frame = new byte[bytes.Length + 1];
            frame[0] = DistributionRawInputHostNames.PassThrough;
            bytes.CopyTo(frame, 1);
            await input.Invoke(context =>
            {
                try
                {
                    _ = DistributionInputAdapter.PutData(context, opaque, new BitString(frame));
                    Check(false);
                }
                catch (NotSupportedException)
                {
                    Check(true);
                }

                return true;
            });
        }

        int DoomedControllers() => ((IEnumerable<ProcessContext>)typeof(RuntimeDistributionPending)
            .GetField(
                DistributionRawInputHostNames.ExitingControllers,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            )!
            .GetValue(pending)!).Count();

        void Expect(string reason, Action action)
        {
            try
            {
                action();
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(reason)))
            {
                Check(true);

                return;
            }
            Check(false);
        }

        void Check(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException(DistributionRawInputHostNames.Failure);
            checks++;
        }
    }
}
