using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionPayloadExitHostChecks
{
    internal static async Task Run()
    {
        CheckEncoderOptions();
        foreach (int opcode in new[]
        {
            DistributionControlOpcodes.PayloadExit,
            DistributionControlOpcodes.PayloadExitTrace,
            DistributionControlOpcodes.PayloadExit2,
            DistributionControlOpcodes.PayloadExit2Trace
        })
            await CheckTrapped(opcode);
        await CheckDrops();
        await CheckNormal();
        await CheckMalformed(false);
        await CheckMalformed(true);
        await CheckKill();
        await CheckFactoryTimingAndDrops();
    }

    private static void CheckEncoderOptions()
    {
        var remote = new Pid(DistributionPayloadExitHostNames.Remote, 1, 0);
        var local = new Pid(DistributionPayloadExitHostNames.Local, 2, 4);
        var control = Control(DistributionControlOpcodes.PayloadExit2, remote, local);
        foreach (var options in new[]
        {
            new EtfEncodingOptions { Local = true, LocalNode = DistributionPayloadExitHostNames.Local },
            new EtfEncodingOptions { CompressionLevel = DistributionPayloadExitHostNames.Compression }
        })
        {
            try
            {
                _ = DistributionPassThroughCodec.EncodePayloadExit(control, Nil.Value, options: options);
                Check(false);
            }
            catch (NotSupportedException)
            {
                Check(true);
            }
        }
    }

    private static async Task CheckFactoryTimingAndDrops()
    {
        await WithConnection(async (
            runtime,
            pending,
            handle,
            put,
            receiver,
            remote
        ) =>
        {
            var owner = await receiver.Ready;
            int calls = 0;
            bool observe = false;
            var identity = new LocalFunctionIdentity(
                Term.A(DistributionPayloadExitHostNames.FunctionModule),
                null,
                DistributionPayloadExitHostNames.FunctionIndex,
                0,
                new BitString(new byte[DistributionPayloadExitHostNames.FunctionUniqueBytes])
            );
            runtime.Functions.Register(
                identity,
                0,
                0,
                environment =>
            {
                if (observe)
                {
                    calls++;

                    object Gate(object value) => value.GetType().GetField(DistributionPayloadExitHostNames.Gate,
                        BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
                    Check(!Monitor.IsEntered(Gate(runtime)) && !Monitor.IsEntered(Gate(owner.Mailbox)));
                    // Same-owner snapshot cannot consume later signals from within the factory.
                    Check(owner.Mailbox.Count == 0);
                }

                return new FunctionTerm(
                    identity,
                    0,
                    environment,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
            }
            );
            FunctionTerm function = runtime.Functions.Restore(
                runtime,
                identity,
                0,
                []
            );
            byte[] reason = ExternalTermFormat.Encode(function, new EtfEncodingOptions { FunctionOwner = owner.Self });
            observe = true;
            await put(Frame(Control(DistributionControlOpcodes.PayloadExit, remote, owner.Self), reason));
            await Drain(receiver);
            Check(calls == 0);
            var missing = new Pid(owner.Self.Node, DistributionPayloadExitHostNames.MissingId, owner.Self.Creation);
            await put(Frame(Control(DistributionControlOpcodes.PayloadExit2, remote, missing), reason));
            Check(calls == 0);
            await receiver.Invoke(context =>
 {
     context.TrapExits = true;

     return true;
 });
            await put(Frame(Control(DistributionControlOpcodes.PayloadExit2, remote, owner.Self), reason));
            Check(calls == 0);
            Term? message = await receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(
                value => value, DistributionPayloadExitHostNames.Deadline));
            Check(calls == 1);
            Check(message is TupleTerm tuple && tuple.Items.Count == 3 && tuple.Items[2].Equals(function));
            Check(State(pending, handle) == DistributionPendingState.Connected);
        });
    }

    private static async Task CheckTrapped(int opcode)
    {
        await WithConnection(async (
            runtime,
            pending,
            handle,
            put,
            receiver,
            remote
        ) =>
        {
            await receiver.Invoke(context =>
 {
     context.TrapExits = true;

     return true;
 });
            if (opcode is DistributionControlOpcodes.PayloadExit or DistributionControlOpcodes.PayloadExitTrace)
            {
                await put(DistributionPassThroughCodec.EncodeProcessControl(
                    Term.Tuple(Term.I(DistributionControlOpcodes.Link), remote, receiver.Process.Pid)));
                await Drain(receiver);
            }
            var control = Control(opcode, remote, receiver.Process.Pid);
            await put(DistributionPassThroughCodec.EncodePayloadExit(control, DistributionPayloadExitHostNames.Value));
            Check(State(pending, handle) == DistributionPendingState.Connected);
            Term? received = await receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(
                message => message, DistributionPayloadExitHostNames.Deadline));
            Check(received!.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), remote, DistributionPayloadExitHostNames.Value)));
            Check(!receiver.Process.Completion.IsCompleted);
            Check(State(pending, handle) == DistributionPendingState.Connected);
        });
    }

    private static async Task CheckDrops()
    {
        await WithConnection(async (
            runtime,
            pending,
            handle,
            put,
            receiver,
            remote
        ) =>
        {
            byte[] invalid = [DistributionPayloadExitHostNames.EtfVersion];
            var missing = new Pid(receiver.Process.Pid.Node, DistributionPayloadExitHostNames.MissingId, receiver.Process.Pid.Creation);
            await put(Frame(Control(DistributionControlOpcodes.PayloadExit2, remote, missing), invalid));
            await put(Frame(Control(DistributionControlOpcodes.PayloadExit, remote, receiver.Process.Pid), invalid));
            await Drain(receiver);
            Check(State(pending, handle) == DistributionPendingState.Connected);
            Check(!receiver.Process.Completion.IsCompleted);
            Check((await receiver.Ready).Mailbox.Count == 0);
            await put(DistributionPassThroughCodec.EncodeProcessControl(
                Term.Tuple(Term.I(DistributionControlOpcodes.Link), remote, receiver.Process.Pid)));
            await Drain(receiver);
            await put(Frame(Control(DistributionControlOpcodes.PayloadExit, remote, receiver.Process.Pid), invalid));
            runtime.Unlink(await receiver.Ready, remote);
            await Drain(receiver);
            Check(State(pending, handle) == DistributionPendingState.Connected);
            Check(!receiver.Process.Completion.IsCompleted);
            Check((await receiver.Ready).Mailbox.Count == 0);
        });
    }

    private static async Task CheckNormal()
    {
        await WithConnection(async (
            runtime,
            pending,
            handle,
            put,
            receiver,
            remote
        ) =>
        {
            await put(DistributionPassThroughCodec.EncodePayloadExit(
                Control(DistributionControlOpcodes.PayloadExit2, remote, receiver.Process.Pid), Term.A(ProcessExitReasons.Normal)));
            await Drain(receiver);
            Check(!receiver.Process.Completion.IsCompleted);
            Check((await receiver.Ready).Mailbox.Count == 0);
            Check(State(pending, handle) == DistributionPendingState.Connected);
        });
    }

    private static async Task CheckMalformed(bool replace)
    {
        await WithConnection(async (
            runtime,
            pending,
            handle,
            put,
            receiver,
            remote
        ) =>
        {
            await put(Frame(
                Control(DistributionControlOpcodes.PayloadExit2, remote, receiver.Process.Pid),
                [DistributionPayloadExitHostNames.EtfVersion]
            ));
            Check(State(pending, handle) == DistributionPendingState.Connected);
            if (replace)
            {
                pending.CloseConnected(handle);
                handle = pending.BeginPending(Term.A(DistributionPayloadExitHostNames.Remote));
                // A replacement has no controller yet: the stale decoder must not close it.
                Check(State(pending, handle) == DistributionPendingState.Pending);
            }
            await Drain(receiver);
            Check(State(pending, handle) == (replace ? DistributionPendingState.Pending : DistributionPendingState.Exiting));
            Check(!receiver.Process.Completion.IsCompleted);
        });
    }

    private static async Task CheckKill()
    {
        await WithConnection(async (
            runtime,
            pending,
            handle,
            put,
            receiver,
            remote
        ) =>
        {
            await receiver.Invoke(context =>
 {
     context.TrapExits = true;

     return true;
 });
            await put(DistributionPassThroughCodec.EncodePayloadExit(
                Control(DistributionControlOpcodes.PayloadExit2Trace, remote, receiver.Process.Pid), Term.A(ProcessExitSignals.Kill)));
            // Drive the real signal drain directly; actor command completion may observe cancellation.
            _ = (await receiver.Ready).Mailbox.Count;
            Check((await receiver.Process.Completion.WaitAsync(DistributionPayloadExitHostNames.Deadline))
                .Equals(Term.A(ProcessExitReasons.Killed)));
            Check(State(pending, handle) == DistributionPendingState.Connected);
        });
    }

    private static async Task WithConnection(
        Func<ProcessRuntime, RuntimeDistributionPending, Term, Func<byte[], Task<Term>>, RuntimeNodeFoundationHostActor, Pid, Task> body
    )
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            _ = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionPayloadExitHostNames.Local),
                Term.I(0)
            );

            return true;
        });
        var pending = runtime.DistributionPending;
        Term handle = pending.BeginPending(Term.A(DistributionPayloadExitHostNames.Remote));
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            Term.A(DistributionPayloadExitHostNames.Remote),
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload), Term.I(0))
        ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid));
        var remote = new Pid(DistributionPayloadExitHostNames.Remote, 1, 0);
        await body(
            runtime,
            pending,
            handle,
            bytes => input.Invoke(context => DistributionInputAdapter.PutData(context, opaque, new BitString(bytes))),
            receiver,
            remote
        );
    }

    private static TupleTerm Control(int opcode, Pid remote, Pid target)
        => opcode is DistributionControlOpcodes.PayloadExitTrace or DistributionControlOpcodes.PayloadExit2Trace
            ? Term.Tuple(
                Term.I(opcode),
                remote,
                target,
                Nil.Value
            )
            : Term.Tuple(Term.I(opcode), remote, target);

    private static byte[] Frame(TupleTerm control, byte[] payload)
    {
        byte[] encoded = ExternalTermFormat.Encode(control);
        byte[] result = new byte[DistributionPassThroughLayout.MarkerBytes + encoded.Length + payload.Length];
        result[0] = DistributionPassThroughLayout.Marker;
        encoded.CopyTo(result, DistributionPassThroughLayout.MarkerBytes);
        payload.CopyTo(result, DistributionPassThroughLayout.MarkerBytes + encoded.Length);

        return result;
    }

    private static Task<Term?> Drain(RuntimeNodeFoundationHostActor receiver)
        => receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero));

    private static DistributionPendingState State(RuntimeDistributionPending pending, Term handle)
    {
        using var lease = pending.Acquire(handle);

        return lease.Snapshot().State;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionPayloadExitHostNames.Failure);
    }
}
