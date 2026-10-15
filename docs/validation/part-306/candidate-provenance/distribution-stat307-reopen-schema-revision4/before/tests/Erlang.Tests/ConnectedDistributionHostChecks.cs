using System.Reflection;
using System.Text.Json;
using Erlang;
using Erlang.Tests;

namespace Erlang.Tests;

internal static class ConnectedDistributionHostChecks
{
    internal static async Task Run()
    {
        int checks = 0;
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var recipient = new RuntimeNodeFoundationHostActor(runtime);
        await using var outsider = new RuntimeNodeFoundationHostActor(foreign);
        Check(kernel.Process.Pid.Equals(outsider.Process.Pid));
        var controllerLookup = typeof(ProcessRuntime).GetMethod(ConnectedDistributionHostNames.ControllerLookup, BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check(ReferenceEquals(controllerLookup.Invoke(runtime, [kernel.Process.Pid]), await kernel.Ready));
        try
        {
            _ = controllerLookup.Invoke(runtime, [outsider.Process.Pid]);
            throw new Exception(ConnectedDistributionHostNames.Failure);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ErlangException reason &&
            reason.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            checks++;
        }
        var pending = runtime.DistributionPending;
        var remote = Term.A(ConnectedDistributionHostNames.Remote);
        var handle = pending.BeginPending(remote);
        var futureSelf = pending.BeginPending(Term.A(ConnectedDistributionHostNames.Local));
        await kernel.Invoke(context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            pending.ReserveKernelSetup(context);

            return true;
        });
        await controller.Invoke(context => Reject(() => pending.AdoptConnected(
            handle,
            context,
            DistributionPassThroughFlags.Mandatory,
            1
        )));
        Check(!runtime.IsDistributionAlive);
        await controller.Invoke(context => RejectErlang(() => DistributionControllerPrimitives.GetOption(context, Nil.Value, Nil.Value), DistributionControllerNames.NotSupported));
        await kernel.Invoke(context =>
        {
            var method = typeof(ProcessRuntime).GetMethod(ConnectedDistributionHostNames.Admission, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check(((Term)method.Invoke(runtime, [context, Term.A(ConnectedDistributionHostNames.Local), Term.I(0)])!).Equals(Term.A(ErlangBooleanAtoms.True)));

            return true;
        });
        Check(runtime.IsDistributionAlive);
        await outsider.Invoke(context =>
        {
            foreign.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            var method = typeof(ProcessRuntime).GetMethod(ConnectedDistributionHostNames.Admission, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check(((Term)method.Invoke(foreign, [context, Term.A(ConnectedDistributionHostNames.Local), Term.I(0)])!).Equals(Term.A(ErlangBooleanAtoms.True)));

            return true;
        });
        Check(outsider.Process.Pid.Node == kernel.Process.Pid.Node && outsider.Process.Pid.Creation == kernel.Process.Pid.Creation);

        await controller.Invoke(context =>
        {
            Reject(() => pending.AdoptConnected(
                futureSelf,
                context,
                DistributionPassThroughFlags.Mandatory,
                1
            ));
            Reject(() => pending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory & ~DistributionPassThroughFlags.MapTag,
                1
            ));
            Reject(() => pending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.AtomCacheHeader,
                1
            ));
            Reject(() => pending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.Fragments,
                1
            ));
            Check(pending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory,
                1
            ).Equals(handle));
            Reject(() => pending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory,
                1
            ));

            return true;
        });
        Check(pending.BeginPending(remote).Equals(handle));
        using (var lease = pending.Acquire(handle))
            Check(lease.Snapshot().State == DistributionPendingState.Connected);
        Check(!pending.AbortPending(handle));
        Check(pending.ConnectedSnapshot(handle).InputHandler is null);
        await kernel.Invoke(context =>
        {
            RejectErlang(
                () => DistributionControllerPrimitives.GetOption(context, handle, Term.A(DistributionControllerNames.GetSize)),
                ErlangErrorReasons.BadArgument
            );
            RejectErlang(
                () => DistributionControllerPrimitives.SetOption(
                    context,
                    handle,
                    Term.A(DistributionControllerNames.GetSize),
                    Term.A(ErlangBooleanAtoms.True)
                ),
                ErlangErrorReasons.BadArgument
            );
            RejectErlang(() => DistributionControllerPrimitives.GetData(context, handle), ErlangErrorReasons.BadArgument);
            RejectErlang(() => DistributionControllerPrimitives.RequestDataNotification(context, handle), ErlangErrorReasons.BadArgument);

            return true;
        });
        await controller.Invoke(context =>
        {
            Check(DistributionControllerPrimitives.GetData(context, handle).Equals(Term.A(DistributionControllerNames.NoData)));
            RejectErlang(
                () => DistributionControllerPrimitives.GetOption(context, Nil.Value, Term.A(DistributionControllerNames.GetSize)),
                ErlangErrorReasons.BadArgument
            );
            RejectErlang(
                () => DistributionControllerPrimitives.SetInputHandler(context, Nil.Value, context.Self),
                DistributionControllerNames.NotSupported
            );
            RejectErlang(() => DistributionControllerPrimitives.SetInputHandler(context, handle, Nil.Value), ErlangErrorReasons.BadArgument);
            RejectErlang(
                () => DistributionControllerPrimitives.SetInputHandler(context, handle, outsider.Process.Pid),
                ErlangErrorReasons.BadArgument
            );
            var fixedLocalHandler = new Pid(outsider.Process.Pid.Node, outsider.Process.Pid.Id, outsider.Process.Pid.Creation);
            Check(DistributionControllerPrimitives.SetInputHandler(context, handle, fixedLocalHandler).Equals(Term.A(ConnectedDistributionHostNames.Success)));


            return true;
        });
        await kernel.InvokeAsync(async context =>
        {
            var notice = Term.Tuple(
                Term.A(ConnectedDistributionHostNames.Notice),
                controller.Process.Pid,
                remote,
                controller.Process.Pid
            );
            Check(await context.ReceiveAsync<Term>(message => message.Equals(notice) ? message : null, ConnectedDistributionHostNames.Deadline) is not null);

            return true;
        });
        await outsider.Invoke(context => Reject(() => pending.SetConnectedGetSize(handle, context, true)));
        await input.Invoke(context => Reject(() => pending.SetConnectedGetSize(handle, context, true)));
        await controller.Invoke(context =>
        {
            Check(!pending.ConnectedGetSize(handle, context));
            Check(DistributionControllerPrimitives.GetOption(context, handle, Term.A(DistributionControllerNames.GetSize)).Equals(Term.A(ErlangBooleanAtoms.False)));
            RejectErlang(
                () => DistributionControllerPrimitives.SetOption(
                    context,
                    handle,
                    Term.A(DistributionControllerNames.GetSize),
                    Nil.Value
                ),
                ErlangErrorReasons.BadArgument
            );
            Check(DistributionControllerPrimitives.SetOption(
                context,
                handle,
                Term.A(DistributionControllerNames.GetSize),
                Term.A(ErlangBooleanAtoms.True)
            ).Equals(Term.A(ErlangBooleanAtoms.False)));
            Check(DistributionControllerPrimitives.SetOption(
                context,
                handle,
                Term.A(DistributionControllerNames.GetSize),
                Term.A(ErlangBooleanAtoms.False)
            ).Equals(Term.A(ErlangBooleanAtoms.True)));
            Check(!pending.SetConnectedGetSize(handle, context, true));
            Check(pending.SetConnectedGetSize(handle, context, false));
            Check(!pending.ConnectedGetSize(handle, context));

            return true;
        });
        await recipient.Invoke(context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Sink, context.Self);

            return true;
        });
        var payload = Term.Tuple(Term.A(ConnectedDistributionHostNames.Payload), new BitString(new byte[] { 0, 128, 255 }));
        var send = DistributionPassThroughCodec.Encode(DistributionPassThroughCodec.Send(recipient.Process.Pid, payload));
        await controller.Invoke(context => Reject(() => pending.AcceptConnectedPassThroughInput(handle, context, send)));
        await controller.Invoke(context =>
        {
            pending.SetConnectedInputHandler(handle, context, input.Process.Pid);
            Check(DistributionControllerPrimitives.SetInputHandler(context, handle, input.Process.Pid).Equals(Term.A(ConnectedDistributionHostNames.Success)));

            return true;
        });
        await controller.Invoke(context => Reject(() => pending.AcceptConnectedPassThroughInput(handle, context, send)));
        await input.Invoke(context =>
        {
            pending.AcceptConnectedPassThroughInput(handle, context, send);
            var registered = DistributionPassThroughCodec.Encode(DistributionPassThroughCodec.SendRegistered(controller.Process.Pid, Term.A(ConnectedDistributionHostNames.Sink), payload));
            pending.AcceptConnectedPassThroughInput(handle, context, registered);
            Reject(() => pending.AcceptConnectedPassThroughInput(handle, context, new byte[] { 112 }));

            return true;
        });
        await recipient.InvokeAsync(async context =>
        {
            for (int index = 0; index < 2; index++)
                Check(await context.ReceiveAsync<Term>(message => message.Equals(payload) ? message : null, ConnectedDistributionHostNames.Deadline) is not null);

            return true;
        });
        Check(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 2);
        await input.Invoke(context =>
        {
            var nonlocal = DistributionPassThroughCodec.Encode(DistributionPassThroughCodec.Send(new Pid(ConnectedDistributionHostNames.Remote, 10, 1), payload));
            pending.AcceptConnectedPassThroughInput(handle, context, nonlocal);

            return true;
        });
        Check(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 3);
        await recipient.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero) is null);

            return true;
        });
        var externalFunction = runtime.Functions.External(
            runtime,
            new ExternalFunctionIdentity(
            Term.A(ErlangModuleNames.Module),
            Term.A(ConnectedDistributionHostNames.SelfFunction),
            0
        )
        );
        var functionIdentity = new LocalFunctionIdentity(
            Term.A(ConnectedDistributionHostNames.FunctionModule),
            null,
            ConnectedDistributionHostNames.FunctionIndex,
            ConnectedDistributionHostNames.FunctionOldUnique,
            new BitString(Enumerable.Range(0, 16).Select(value => (byte)value).ToArray())
        );
        _ = runtime.Functions.Register(
            functionIdentity,
            ConnectedDistributionHostNames.FunctionArity,
            ConnectedDistributionHostNames.FunctionEnvironmentCount,
            environment =>
            {
                Task.Run(() => runtime.Modules.Apply(registry =>
         {
             _ = runtime.WhereIs(ConnectedDistributionHostNames.Kernel);
         }))
                    .WaitAsync(ConnectedDistributionHostNames.Deadline).GetAwaiter().GetResult();

                return new FunctionTerm(
                    functionIdentity,
                    ConnectedDistributionHostNames.FunctionArity,
                    environment,
                    (_, arguments) => ValueTask.FromResult<Term>(Term.Tuple(environment[0], arguments[0]))
                );
            }
        );
        var localFunction = runtime.Functions.Restore(
            runtime,
            functionIdentity,
            ConnectedDistributionHostNames.FunctionArity,
            [payload]
        );
        await input.Invoke(context =>
        {
            pending.AcceptConnectedPassThroughInput(
                handle,
                context,
                DistributionPassThroughCodec.Encode(
                DistributionPassThroughCodec.Send(recipient.Process.Pid, externalFunction)
            )
            );
            var localMessage = DistributionPassThroughCodec.Send(recipient.Process.Pid, localFunction);
            byte[] localControl = ExternalTermFormat.Encode(localMessage.Control);
            byte[] localPayload = ExternalTermFormat.Encode(localMessage.Payload, new EtfEncodingOptions { FunctionOwner = context.Self });
            byte[] localPacket = [DistributionPassThroughLayout.Marker, .. localControl, .. localPayload];
            pending.AcceptConnectedPassThroughInput(handle, context, localPacket);

            return true;
        });
        await recipient.InvokeAsync(async context =>
        {
            var restoredExternal = await context.ReceiveAsync<FunctionTerm>(message => message is FunctionTerm { ExternalIdentity: not null } function ? function : null, ConnectedDistributionHostNames.Deadline);
            Check(restoredExternal is not null && (await restoredExternal.Invoke(context, [])).Equals(context.Self));
            var restoredLocal = await context.ReceiveAsync<FunctionTerm>(message => message is FunctionTerm { LocalIdentity: not null } function ? function : null, ConnectedDistributionHostNames.Deadline);
            Check(restoredLocal is not null && (await restoredLocal.Invoke(context, [Term.I(17)])).Equals(Term.Tuple(payload, Term.I(17))));

            return true;
        });
        Check(pending.ConnectedSnapshot(handle).AcceptedInputPackets == 5);
        await controller.Invoke(context =>
        {
            pending.RequestConnectedOutputNotification(handle, context);
            Check(DistributionControllerPrimitives.RequestDataNotification(context, handle).Equals(Term.A(ConnectedDistributionHostNames.Success)));
            pending.RequestConnectedOutputNotification(handle, context);
            Check(pending.ConnectedSnapshot(handle).NotificationArmed);

            return true;
        });
        var outgoing = DistributionPassThroughCodec.SendRegistered(recipient.Process.Pid, Term.A(ConnectedDistributionHostNames.Sink), payload);
        var expected = DistributionPassThroughCodec.Encode(outgoing);
        pending.EnqueueConnectedOutput(handle, outgoing);
        var queued = pending.ConnectedSnapshot(handle);
        Check(queued.QueuedPacketBytes == expected.Length && queued.QueuedPackets == 1 && queued.ConsumedOutputPackets == 0 && !queued.NotificationArmed);
        await controller.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(
                message => message.Equals(Term.A(ConnectedDistributionHostNames.Data)) ? message : null,
                ConnectedDistributionHostNames.Deadline
            ) is not null);
            _ = pending.SetConnectedGetSize(handle, context, true);
            var buffer = pending.TakeConnectedOutput(handle, context, out bool getSize);
            Check(getSize && buffer is not null && buffer.PayloadBytes == expected.Length);
            Check(buffer!.Segments.SelectMany(segment => segment.ToArray()).SequenceEqual(expected));
            Check(pending.TakeConnectedOutput(handle, context, out _) is null);
            Check(DistributionControllerPrimitives.GetData(context, handle).Equals(Term.A(DistributionControllerNames.NoData)));

            return true;
        });
        var empty = pending.ConnectedSnapshot(handle);
        Check(empty.QueuedPacketBytes == 0 && empty.QueuedPackets == 0 && empty.ConsumedOutputPackets == 1);
        pending.EnqueueConnectedOutput(handle, outgoing);
        await controller.Invoke(context =>
        {
            Term sized = DistributionControllerPrimitives.GetData(context, handle);
            Check(sized.Equals(Term.Tuple(Term.I(expected.Length), Term.List(new BitString(expected)))));
            Check(DistributionControllerPrimitives.GetData(context, handle).Equals(Term.A(DistributionControllerNames.NoData)));
            Check(pending.ConnectedSnapshot(handle).ConsumedOutputPackets == 2);

            return true;
        });
        var outgoingLocalFunction = DistributionPassThroughCodec.Send(recipient.Process.Pid, localFunction);
        RejectErlang(() => pending.EnqueueConnectedOutput(handle, outgoingLocalFunction), ErlangErrorReasons.BadArgument);
        await outsider.Invoke(context => Reject(() => pending.EnqueueConnectedOutput(handle, context, outgoingLocalFunction)));
        await input.Invoke(context =>
        {
            pending.EnqueueConnectedOutput(handle, context, outgoingLocalFunction);

            return true;
        });
        byte[] expectedLocalControl = ExternalTermFormat.Encode(outgoingLocalFunction.Control);
        byte[] expectedLocalPayload = ExternalTermFormat.Encode(outgoingLocalFunction.Payload, new EtfEncodingOptions { FunctionOwner = input.Process.Pid });
        byte[] expectedLocalOutput = [DistributionPassThroughLayout.Marker, .. expectedLocalControl, .. expectedLocalPayload];
        byte[] actualLocalOutput = await controller.Invoke(context =>
        {
            var buffer = pending.TakeConnectedOutput(handle, context, out _);
            Check(buffer is not null);

            return buffer!.Segments.SelectMany(segment => segment.ToArray()).ToArray();
        });
        Check(actualLocalOutput.SequenceEqual(expectedLocalOutput));
        await input.Invoke(context =>
        {
            pending.AcceptConnectedPassThroughInput(handle, context, actualLocalOutput);

            return true;
        });
        await recipient.InvokeAsync(async context =>
        {
            var function = await context.ReceiveAsync<FunctionTerm>(message => message is FunctionTerm { LocalIdentity: not null } value ? value : null, ConnectedDistributionHostNames.Deadline);
            Check(function is not null && (await function.Invoke(context, [Term.I(19)])).Equals(Term.Tuple(payload, Term.I(19))));

            return true;
        });
        pending.CloseConnected(handle);
        Reject(() => pending.ConnectedSnapshot(handle));
        var next = pending.BeginPending(remote);
        Check(!next.Equals(handle));
        await controller.Invoke(context => Reject(() => pending.AdoptConnected(
            handle,
            context,
            DistributionPassThroughFlags.Mandatory,
            1
        )));
        await controller.Invoke(context =>
        {
            _ = pending.AdoptConnected(
                next,
                context,
                DistributionPassThroughFlags.Mandatory,
                2
            );

            return true;
        });
        pending.CloseConnected(next);
        var rpcPending = pending.BeginPending(remote);
        var adoptionStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adoptionTask = input.InvokeAsync(async context =>
        {
            var adoption = DistributionControllerAdapters.SetNode(
                context,
                remote,
                controller.Process.Pid,
                Term.Tuple(Term.I(checked((long)DistributionPassThroughFlags.Mandatory)), Term.I(3))
            );
            adoptionStarted.TrySetResult(true);

            return await adoption;
        });
        await adoptionStarted.Task.WaitAsync(ConnectedDistributionHostNames.Deadline);
        await controller.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);

            return true;
        });
        Term opaque = await adoptionTask;
        Check(opaque is TupleTerm && opaque.Equals(rpcPending));
        await kernel.InvokeAsync(async context =>
        {
            var notice = Term.Tuple(
                Term.A(ConnectedDistributionHostNames.Notice),
                controller.Process.Pid,
                remote,
                input.Process.Pid
            );
            Check(await context.ReceiveAsync<Term>(message => message.Equals(notice) ? message : null, ConnectedDistributionHostNames.Deadline) is not null);

            return true;
        });
        await controller.Invoke(context =>
        {
            Check(DistributionControllerAdapters.GetData(context, opaque).Equals(Term.A(DistributionControllerNames.NoData)));
            Check(DistributionControllerAdapters.GetData(context, rpcPending).Equals(Term.A(DistributionControllerNames.NoData)));
            Check(DistributionControllerAdapters.GetOption(context, opaque, Term.A(DistributionControllerNames.GetSize)).Equals(Term.A(ErlangBooleanAtoms.False)));
            Check(DistributionControllerAdapters.SetOption(
                context,
                opaque,
                Term.A(DistributionControllerNames.GetSize),
                Term.A(ErlangBooleanAtoms.True)
            ).Equals(Term.A(ErlangBooleanAtoms.False)));
            Check(DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid).Equals(Term.A(ConnectedDistributionHostNames.Success)));

            return true;
        });
        pending.CloseConnected(rpcPending);
        var reopenPending = pending.BeginPending(remote);
        Term reopenedOpaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            remote,
            context.Self,
            Term.Tuple(Term.I(checked((long)DistributionPassThroughFlags.Mandatory)), Term.I(4))
        ));
        Check(reopenedOpaque is ReferenceTerm && !reopenedOpaque.Equals(opaque));
        await controller.Invoke(context =>
        {
            RejectErlang(() => DistributionControllerAdapters.GetData(context, opaque), ErlangErrorReasons.BadArgument);
            Check(DistributionControllerAdapters.GetData(context, reopenedOpaque).Equals(Term.A(DistributionControllerNames.NoData)));

            return true;
        });
        pending.CloseConnected(reopenPending);
        var abortedPending = pending.BeginPending(remote);
        var abortionQueued = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var abortedAdoption = input.InvokeAsync(async context =>
        {
            var adoption = DistributionControllerAdapters.SetNode(
                context,
                remote,
                controller.Process.Pid,
                Term.Tuple(Term.I(checked((long)DistributionPassThroughFlags.Mandatory)), Term.I(5))
            );
            abortionQueued.TrySetResult(true);

            return await adoption;
        });
        await abortionQueued.Task.WaitAsync(ConnectedDistributionHostNames.Deadline);
        Check(pending.AbortPending(abortedPending));
        try
        {
            _ = await abortedAdoption;
            throw new Exception(ConnectedDistributionHostNames.Failure);
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            checks++;
        }
        await controller.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);

            return true;
        });
        Check(runtime.IsAlive(controller.Process.Pid));
        var orphanedPending = pending.BeginPending(remote);
        var requesterQueued = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var departedRequester = runtime.Spawn(async context =>
        {
            var adoption = DistributionControllerAdapters.SetNode(
                context,
                remote,
                controller.Process.Pid,
                Term.Tuple(Term.I(checked((long)DistributionPassThroughFlags.Mandatory)), Term.I(5))
            );
            requesterQueued.TrySetResult(true);

            return await adoption;
        });
        await requesterQueued.Task.WaitAsync(ConnectedDistributionHostNames.Deadline);
        await input.Invoke(context =>
        {
            runtime.Exit(context, departedRequester.Pid, Term.A(ProcessExitSignals.Kill));

            return true;
        });
        Check((await departedRequester.Completion.WaitAsync(ConnectedDistributionHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Killed)));
        await controller.InvokeAsync(async context =>
        {
            Check(await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero) is null);

            return true;
        });
        Check(pending.ConnectedSnapshot(orphanedPending).Controller.Equals(controller.Process.Pid));
        await controller.Invoke(context =>
        {
            pending.SetConnectedInputHandler(orphanedPending, context, departedRequester.Pid);
            Check(pending.ConnectedSnapshot(orphanedPending).InputHandler!.Equals(departedRequester.Pid));

            return true;
        });

        await kernel.InvokeAsync(async context =>
        {
            var notice = Term.Tuple(
                Term.A(ConnectedDistributionHostNames.Notice),
                controller.Process.Pid,
                remote,
                departedRequester.Pid
            );
            Check(await context.ReceiveAsync<Term>(message => message.Equals(notice) ? message : null, ConnectedDistributionHostNames.Deadline) is not null);

            return true;
        });
        next = orphanedPending;
        await kernel.DisposeAsync();
        Check(!runtime.IsDistributionAlive);
        Reject(() => pending.ConnectedSnapshot(next));
        using (var lease = pending.Acquire(next))
            Check(lease.Snapshot().State == DistributionPendingState.Idle);
        checks += await ConnectedDistributionEventsHostChecks.Run();
        if (checks != ConnectedDistributionHostNames.ExpectedAssertions)
            throw new InvalidOperationException(ConnectedDistributionHostNames.Failure);
        Console.WriteLine(JsonSerializer.Serialize(new { Checks = checks, Failed = 0, NativeExecuted = false, CoreRegistrationsAdded = 0 }));

        void Check(bool condition)
        {
            checks++;
            if (!condition)
                throw new Exception(ConnectedDistributionHostNames.Failure);
        }

        bool Reject(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
            {
                checks++;

                return true;
            }
            throw new Exception(ConnectedDistributionHostNames.Failure);
        }

        bool RejectErlang(Action action, string reason)
        {
            try
            {
                action();
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A(reason)))
            {
                checks++;

                return true;
            }
            throw new Exception(ConnectedDistributionHostNames.Failure);
        }
    }
}
