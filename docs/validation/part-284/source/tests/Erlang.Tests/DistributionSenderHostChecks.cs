using Erlang;

namespace Erlang.Tests;

internal sealed class DistributionSenderHostChecks
{
    private int checks;

    internal static async Task Run()
    {
        var session = new DistributionSenderHostChecks();
        session.CheckCodecBoundaries();
        await session.RunActors();
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            SenderAssertions = session.checks,
            NativeExecuted = false
        }));
    }

    private async Task RunActors()
    {
        await using var runtime = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var input = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.InvokeAsync(async context =>
        {
            runtime.Register(DistributionSenderHostNames.Kernel, context.Self);
            await Call(
                context,
                DistributionSenderHostNames.SetNode,
                Term.A(DistributionSenderHostNames.Local),
                Term.I(0)
            );

            return true;
        });
        var pending = runtime.DistributionPending;
        Term handle = pending.BeginPending(Term.A(DistributionSenderHostNames.Remote));
        ulong flags = DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.SendSender;
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            Term.A(DistributionSenderHostNames.Remote),
            context.Self,
            Term.Tuple(new Integer(flags), Term.I(0))
        ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, input.Process.Pid));
        Pid[] senders = [
            new Pid(DistributionSenderHostNames.Remote, 1, 0),
            new Pid(DistributionSenderHostNames.Third, 2, 7),
            controller.Process.Pid
        ];
        foreach (Pid sender in senders)
        {
            await target.InvokeAsync(async context =>
            {
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.A(DistributionSenderHostNames.InitialLabel)
                );

                return true;
            });
            Term initial = await Info(SequenceTraceNames.Label);
            Term token = Token(sender);
            await Put(Control(
                DistributionControlOpcodes.SendSenderTrace,
                sender,
                target.Process.Pid,
                token
            ), DistributionSenderHostNames.First);
            Check((await Info(SequenceTraceNames.Label)).Equals(initial));
            await Put(
                Control(
                    DistributionControlOpcodes.SendSenderTrace,
                    sender,
                    target.Process.Pid,
                    Nil.Value
                ),
                DistributionSenderHostNames.Second
            );
            Term? selected = await target.InvokeAsync(async context => await context.ReceiveAsync(
                message => message.Equals(DistributionSenderHostNames.Second) ? message : null,
                DistributionSenderHostNames.Deadline
            ));
            Check(selected!.Equals(DistributionSenderHostNames.Second));
            Check((await Info(SequenceTraceNames.Label)).Equals(Nil.Value));
            Check((await Receive())!.Equals(DistributionSenderHostNames.First));
            Check((await Info(SequenceTraceNames.Label)).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), DistributionSenderHostNames.Label)));
            Check((await Info(SequenceTraceNames.Serial)).Equals(Term.Tuple(
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(DistributionSenderHostNames.Last), Term.I(DistributionSenderHostNames.Serial))
            )));
            Term previous = await target.InvokeAsync(async context => await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Token),
                Nil.Value
            ));
            Check(previous.Equals(token));
            await Put(Control(
                DistributionControlOpcodes.SendSenderTrace,
                sender,
                target.Process.Pid,
                token
            ), DistributionSenderHostNames.First);
            Check((await Receive())!.Equals(DistributionSenderHostNames.First));
            await Put(
                Control(
                    DistributionControlOpcodes.SendSenderTrace,
                    sender,
                    target.Process.Pid,
                    Term.A(DistributionSenderHostNames.Preserve)
                ),
                DistributionSenderHostNames.Second
            );
            Check((await Receive())!.Equals(DistributionSenderHostNames.Second));
            Check((await Info(SequenceTraceNames.Label)).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), DistributionSenderHostNames.Label)));
            await Put(Control(DistributionControlOpcodes.SendSender, sender, target.Process.Pid), DistributionSenderHostNames.Second);
            Check((await Receive())!.Equals(DistributionSenderHostNames.Second));
            Check((await Info(SequenceTraceNames.Label)).Equals(Nil.Value));
        }
        long attempts = pending.ConnectedSnapshot(handle).RawInputAttempts;
        try
        {
            await controller.Invoke(context => DistributionInputAdapter.PutData(
                context,
                opaque,
                new BitString(Frame(
                Control(DistributionControlOpcodes.SendSender, senders[0], target.Process.Pid),
                DistributionSenderHostNames.Second))
            ));
            Check(false);
        }
        catch (ErlangException exception)
        {
            Check(exception.Reason.Equals(Term.A(DistributionControllerNames.NotSupported)));
        }
        Check(pending.ConnectedSnapshot(handle).RawInputAttempts == attempts);
        ProcessHandle dead = runtime.Spawn(_ => ValueTask.FromResult<Term>(Nil.Value));
        await dead.Completion.WaitAsync(DistributionSenderHostNames.Deadline);
        foreach (Pid recipient in new[] {
            dead.Pid,
            new Pid(runtime.Node, DistributionSenderHostNames.MissingId, target.Process.Pid.Creation),
            new Pid(DistributionSenderHostNames.Third, 3, 7),
            new Pid(runtime.Node, target.Process.Pid.Id, target.Process.Pid.Creation + 1)
        })
            await Put(Control(DistributionControlOpcodes.SendSender, senders[0], recipient), DistributionSenderHostNames.Second);
        Check(!controller.Process.Completion.IsCompleted);
        Check((await target.InvokeAsync(async context => await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero))) is null);
        Check(pending.ConnectedSnapshot(handle).RawInputAttempts == attempts + 4);

        async Task Put(TupleTerm control, Term payload)
        {
            await input.Invoke(context => DistributionInputAdapter.PutData(context, opaque, new BitString(Frame(control, payload))));
        }

        Task<Term> Info(string component) => target.InvokeAsync(async context => await Call(context, SequenceTraceNames.Info, Term.A(component)));

        Task<Term?> Receive() => target.InvokeAsync(async context => await context.ReceiveAsync(message => message, DistributionSenderHostNames.Deadline));
    }

    private void CheckCodecBoundaries()
    {
        var sender = new Pid(DistributionSenderHostNames.Third, 1, 7);
        var target = new Pid(DistributionSenderHostNames.Local, 2, 0);
        TupleTerm control = Control(DistributionControlOpcodes.SendSender, sender, target);
        byte[] wire = Frame(control, DistributionSenderHostNames.Second);
        // Native receiver does not check DFLAG_SEND_SENDER in this switch.
        Check(DistributionPassThroughCodec.DecodeProcessPacket(wire).Control.Equals(control));
        try
        {
            _ = DistributionPassThroughCodec.Encode(new(control, DistributionSenderHostNames.Second));
            Check(false);
        }
        catch (NotSupportedException)
        {
            Check(true);
        }
        ulong flags = DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.SendSender;
        Check(DistributionPassThroughCodec.Encode(new(control, DistributionSenderHostNames.Second), negotiatedFlags: flags).SequenceEqual(wire));
        foreach (TupleTerm malformed in new[] {
            Term.Tuple(Term.I(DistributionControlOpcodes.SendSender), sender, Nil.Value),
            Term.Tuple(Term.I(DistributionControlOpcodes.SendSenderTrace), sender, target),
            Term.Tuple(Term.I(DistributionControlOpcodes.SendSender), Nil.Value, target)
        })
        {
            try
            {
                _ = DistributionPassThroughCodec.DecodeProcessPacket(Frame(malformed, Nil.Value), negotiatedFlags: flags);
                Check(false);
            }
            catch (ArgumentException)
            {
                Check(true);
            }
        }
        foreach (byte[] malformed in new[] { wire[..^1], wire.Concat(new byte[] { 0 }).ToArray() })
        {
            try
            {
                _ = DistributionPassThroughCodec.DecodeProcessPacket(malformed, negotiatedFlags: flags);
                Check(false);
            }
            catch (ArgumentException)
            {
                Check(true);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                Check(true);
            }
        }
    }

    private static TupleTerm Control(
        int opcode,
        Pid sender,
        Pid target,
        Term? token = null
    )
        => token is null ? Term.Tuple(Term.I(opcode), sender, target) : Term.Tuple(
            Term.I(opcode),
            sender,
            target,
            token
        );

    private static TupleTerm Token(Pid sender)
        => Term.Tuple(
            Term.I(DistributionSenderHostNames.WideFlag),
            DistributionSenderHostNames.Label,
            Term.I(DistributionSenderHostNames.Serial),
            sender,
            Term.I(DistributionSenderHostNames.Last)
        );

    private static byte[] Frame(TupleTerm control, Term payload)
    {
        byte[] first = ExternalTermFormat.Encode(control), second = ExternalTermFormat.Encode(payload);
        byte[] frame = new byte[DistributionPassThroughLayout.MarkerBytes + first.Length + second.Length];
        frame[0] = DistributionPassThroughLayout.Marker;
        first.CopyTo(frame, DistributionPassThroughLayout.MarkerBytes);
        second.CopyTo(frame, DistributionPassThroughLayout.MarkerBytes + first.Length);

        return frame;
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionSenderHostNames.Failure);
        Interlocked.Increment(ref checks);
    }
}
