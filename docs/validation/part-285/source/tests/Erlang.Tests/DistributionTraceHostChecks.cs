using Erlang;

namespace Erlang.Tests;

internal static class DistributionTraceHostChecks
{
    internal static async Task Run()
    {
        CheckCodecBoundaries();
        await RunMessages(false);
        await RunMessages(true);
        await RunExitSignals();
    }

    private static async Task RunMessages(bool typed)
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await using var tracer = new RuntimeNodeFoundationHostActor(runtime);
        await Activate(runtime, kernel);
        var pending = runtime.DistributionPending;
        Term handle = pending.BeginPending(Term.A(DistributionTraceHostNames.Remote));
        Term opaque = await Connect(controller, input);
        var remote = new Pid(DistributionTraceHostNames.Remote, 1, 0);
        await receiver.InvokeAsync(async context =>
        {
            runtime.Register(DistributionTraceHostNames.Registered, context.Self);
            await Set(context, SequenceTraceNames.Label, Term.A(DistributionTraceHostNames.InitialLabel));
            await Call(
                context,
                SequenceTraceNames.SystemFlag,
                Term.A(SequenceTraceNames.SystemTracer),
                tracer.Process.Pid
            );

            return true;
        });
        Term initial = await Info(receiver, SequenceTraceNames.Label);
        Term token = Token(remote, DistributionTraceHostNames.ReceiveFlag);
        await Send(DistributionControlOpcodes.SendTrace, token, Term.A(DistributionTraceHostNames.Skipped));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(initial));
        await Send(DistributionControlOpcodes.RegisteredSendTrace, Nil.Value, DistributionTraceHostNames.Message);
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(initial));
        Check((await receiver.InvokeAsync(async context => await context.ReceiveAsync(
            message => message.Equals(DistributionTraceHostNames.Message) ? message : null,
            DistributionTraceHostNames.Deadline)))!.Equals(DistributionTraceHostNames.Message));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(Nil.Value));
        Term skipped = Term.A(DistributionTraceHostNames.Skipped);
        Check((await Receive(receiver))!.Equals(skipped));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(
            Term.Tuple(Term.A(SequenceTraceNames.Label), DistributionTraceHostNames.Label)));
        Check((await Info(receiver, SequenceTraceNames.Serial)).Equals(
            Term.Tuple(
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(DistributionTraceHostNames.Last), Term.I(DistributionTraceHostNames.Serial))
            )));
        Check((await Receive(tracer))!.Equals(Term.Tuple(
            Term.A(SequenceTraceNames.Message),
            DistributionTraceHostNames.Label,
            Term.Tuple(
                Term.A(SequenceTraceNames.Receive),
                Term.Tuple(Term.I(DistributionTraceHostNames.Last), Term.I(DistributionTraceHostNames.Serial)),
                remote,
                receiver.Process.Pid,
                skipped
            )
        )));
        await Send(
            DistributionControlOpcodes.RegisteredSendTrace,
            Term.A(DistributionTraceHostNames.Preserve),
            DistributionTraceHostNames.Message
        );
        Check((await Receive(receiver))!.Equals(DistributionTraceHostNames.Message));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(
            Term.Tuple(Term.A(SequenceTraceNames.Label), DistributionTraceHostNames.Label)));
        await Send(
            DistributionControlOpcodes.SendTrace,
            Token(remote, DistributionTraceHostNames.WideUnknownFlag),
            DistributionTraceHostNames.Message
        );
        Check((await Receive(receiver))!.Equals(DistributionTraceHostNames.Message));
        Term previous = await receiver.InvokeAsync(async context =>
            await Set(context, SequenceTraceNames.Token, Nil.Value));
        Check(previous.Equals(Token(remote, DistributionTraceHostNames.WideUnknownFlag)));
        await Send(DistributionControlOpcodes.RegisteredSendTrace, token, DistributionTraceHostNames.Message);
        Check((await Receive(receiver))!.Equals(DistributionTraceHostNames.Message));
        await Send(DistributionControlOpcodes.Send, Nil.Value, DistributionTraceHostNames.Message);
        Check((await Receive(receiver))!.Equals(DistributionTraceHostNames.Message));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(Nil.Value));
        var missing = new Pid(runtime.Node, DistributionTraceHostNames.MissingId, receiver.Process.Pid.Creation);
        var deadControl = Term.Tuple(
            Term.I(DistributionControlOpcodes.SendTrace),
            Nil.Value,
            missing,
            Term.A(DistributionTraceHostNames.ArbitraryToken)
        );
        await Put(DistributionPassThroughCodec.Encode(new(deadControl, DistributionTraceHostNames.Message)));
        Check(!controller.Process.Completion.IsCompleted);
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(Nil.Value));

        async Task<Term> Connect(RuntimeNodeFoundationHostActor owner, RuntimeNodeFoundationHostActor handler)
        {
            Term result = await owner.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
                context,
                Term.A(DistributionTraceHostNames.Remote),
                context.Self,
                Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload), Term.I(0))
            ));
            await owner.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, result, handler.Process.Pid));

            return result;
        }

        Task<bool> Put(byte[] packet) => input.Invoke(context =>
        {
            if (typed)
                pending.AcceptConnectedPassThroughInput(handle, context, packet);
            else
                _ = DistributionInputAdapter.PutData(context, opaque, new BitString(packet));

            return true;
        });

        Task<bool> Send(int opcode, Term trace, Term payload)
        {
            TupleTerm control = opcode == DistributionControlOpcodes.RegisteredSendTrace
                ? Term.Tuple(
                    Term.I(opcode),
                    remote,
                    Nil.Value,
                    Term.A(DistributionTraceHostNames.Registered),
                    trace
                )
                : opcode == DistributionControlOpcodes.SendTrace
                ? Term.Tuple(
                    Term.I(opcode),
                    Nil.Value,
                    receiver.Process.Pid,
                    trace
                )
                : Term.Tuple(Term.I(opcode), Nil.Value, receiver.Process.Pid);

            return Put(DistributionPassThroughCodec.Encode(new(control, payload)));
        }
    }

    private static async Task RunExitSignals()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await Activate(runtime, kernel);
        Term handle = runtime.DistributionPending.BeginPending(Term.A(DistributionTraceHostNames.Remote));
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            Term.A(DistributionTraceHostNames.Remote),
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), Term.I(0))
        ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid));
        var remote = new Pid(DistributionTraceHostNames.Remote, 1, 0);
        await receiver.InvokeAsync(async context =>
        {
            await Set(context, SequenceTraceNames.Label, Term.A(DistributionTraceHostNames.InitialLabel));

            return true;
        });
        Term initial = await Info(receiver, SequenceTraceNames.Label);
        await Exit(
            DistributionControlOpcodes.Exit2Trace,
            Term.A(DistributionTraceHostNames.ArbitraryToken),
            Term.A(ProcessExitReasons.Normal)
        );
        await Drain();
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(initial));
        Check(!receiver.Process.Completion.IsCompleted);
        await receiver.Invoke(context =>
 {
     context.TrapExits = true;

     return true;
 });
        await Exit(DistributionControlOpcodes.Exit2Trace, Token(remote, 0), Term.A(DistributionTraceHostNames.Reason));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(initial));
        Check((await Receive(receiver))!.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), remote, Term.A(DistributionTraceHostNames.Reason))));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), DistributionTraceHostNames.Label)));
        await Put(DistributionPassThroughCodec.EncodeProcessControl(Term.Tuple(
            Term.I(DistributionControlOpcodes.Link), remote, receiver.Process.Pid)));
        await Drain();
        await Exit(DistributionControlOpcodes.ExitTrace, Nil.Value, Term.A(DistributionTraceHostNames.Reason));
        Check((await Receive(receiver))!.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), remote, Term.A(DistributionTraceHostNames.Reason))));
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(Nil.Value));
        // Removed link: this EXIT is discarded before interpreting its arbitrary token.
        await Exit(
            DistributionControlOpcodes.ExitTrace,
            Term.A(DistributionTraceHostNames.ArbitraryToken),
            Term.A(DistributionTraceHostNames.Reason)
        );
        await Drain();
        Check((await Info(receiver, SequenceTraceNames.Label)).Equals(Nil.Value));
        Check(!controller.Process.Completion.IsCompleted);

        Task<Term> Put(byte[] bytes) => input.Invoke(context => DistributionInputAdapter.PutData(context, opaque, new BitString(bytes)));

        Task<Term> Exit(int opcode, Term trace, Term reason) => Put(DistributionPassThroughCodec.EncodeProcessControl(
            Term.Tuple(
                Term.I(opcode),
                remote,
                receiver.Process.Pid,
                trace,
                reason
            )));

        Task<Term?> Drain() => receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero));
    }

    private static void CheckCodecBoundaries()
    {
        var remote = new Pid(DistributionTraceHostNames.Remote, 1, 0);
        var local = new Pid(DistributionTraceHostNames.Local, 2, 4);
        var reference = new ReferenceTerm(DistributionTraceHostNames.Remote, new uint[] { 1 }, 0);
        var control = Term.Tuple(
            Term.I(DistributionControlOpcodes.PayloadMonitorExit),
            remote,
            local,
            reference
        );
        byte[] frame = Frame(control, Nil.Value);
        int invoked = 0;
        try
        {
            _ = DistributionPassThroughCodec.DecodeProcessPacket(
                frame,
                negotiatedFlags: DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload | DistributionPassThroughFlags.ProcessMonitor,
                unsupportedControlValidator: _ => invoked++
            );
            Check(false);
        }
        catch (NotSupportedException)
        {
            Check(invoked == 1);
        }
        invoked = 0;
        foreach (byte[] malformed in new[] { frame[..^1], frame.Concat(new byte[] { 0 }).ToArray() })
        {
            try
            {
                _ = DistributionPassThroughCodec.DecodeProcessPacket(
                    malformed,
                    negotiatedFlags: DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ExitPayload | DistributionPassThroughFlags.ProcessMonitor,
                    unsupportedControlValidator: _ => invoked++
                );
                Check(false);
            }
            catch (ArgumentException)
            {
                Check(invoked == 0);
            }
        }
    }

    private static byte[] Frame(TupleTerm control, Term payload)
    {
        byte[] first = ExternalTermFormat.Encode(control), second = ExternalTermFormat.Encode(payload);
        byte[] result = new byte[DistributionPassThroughLayout.MarkerBytes + first.Length + second.Length];
        result[0] = DistributionPassThroughLayout.Marker;
        first.CopyTo(result, DistributionPassThroughLayout.MarkerBytes);
        second.CopyTo(result, DistributionPassThroughLayout.MarkerBytes + first.Length);

        return result;
    }

    private static Task<Term?> Receive(RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync(async context => await context.ReceiveAsync(message => message, DistributionTraceHostNames.Deadline));

    private static Task<Term> Info(RuntimeNodeFoundationHostActor actor, string component)
        => actor.InvokeAsync(async context => await Call(context, SequenceTraceNames.Info, Term.A(component)));

    private static ValueTask<Term> Set(ProcessContext context, string component, Term value)
        => Call(
            context,
            SequenceTraceNames.Set,
            Term.A(component),
            value
        );

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static Task<bool> Activate(ProcessRuntime runtime, RuntimeNodeFoundationHostActor kernel)
        => kernel.InvokeAsync(async context =>
        {
            runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);
            await Call(
                context,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionTraceHostNames.Local),
                Term.I(0)
            );

            return true;
        });

    private static TupleTerm Token(Pid sender, long flags)
        => Term.Tuple(
            Term.I(flags),
            DistributionTraceHostNames.Label,
            Term.I(DistributionTraceHostNames.Serial),
            sender,
            Term.I(DistributionTraceHostNames.Last)
        );

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionTraceHostNames.Failure);
    }
}
