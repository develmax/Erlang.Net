using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class DistributionGuestSendHostChecks
{
    internal static async Task Run()
    {
        await PublicRoutesAndCrossApiOrder();
        await SenderAndPriorityProfiles();
        await PressureAndOptions();
        await TraceAdmission();
        await TraceLabelProfiles();
        await CancellationAndGeneration();
        await HaltOwnsBlockedSend();
        await CodecRefusals();
    }

    private static Task PublicRoutesAndCrossApiOrder() => DistributionGuestSendPublicRouteHostChecks.Run();

    private static async Task SenderAndPriorityProfiles()
    {
        await using (var fixture = await DistributionGuestSendHostFixture.Create(DistributionPassThroughFlags.SendSender))
        {
            await Send(fixture, DistributionGuestSendHostNames.First);
            CheckSend(
                await fixture.Take(),
                fixture,
                DistributionGuestSendHostNames.First,
                sender: true
            );
        }
        await using (var fixture = await DistributionGuestSendHostFixture.Create(DistributionPassThroughFlags.AlternativeActionSignal))
        {
            Check((await Send(fixture, DistributionGuestSendHostNames.Second, Options(DistributionGuestSendHostNames.Priority))).Equals(Term.A(DistributionGuestSendHostNames.Ok)));
            var packet = await fixture.Take();
            Check(packet.Control.Equals(Term.Tuple(
                Term.I(DistributionControlOpcodes.AlternativeActionSend),
                Term.I(DistributionAlternativePidSendLayout.Priority),
                fixture.Producer.Process.Pid,
                fixture.Connection.Remote
            )) && packet.Payload!.Equals(Term.A(DistributionGuestSendHostNames.Second)));
        }
        await using (var fixture = await DistributionGuestSendHostFixture.Create())
        {
            await Send(fixture, DistributionGuestSendHostNames.Third, Options(DistributionGuestSendHostNames.Priority));
            CheckSend(
                await fixture.Take(),
                fixture,
                DistributionGuestSendHostNames.Third,
                sender: false
            );
        }
    }

    private static async Task PressureAndOptions()
    {
        await using var fixture = await DistributionGuestSendHostFixture.Create();
        int capacity = fixture.Inner.Fill(fixture.Connection);
        Check(capacity > 0);
        Check((await Send(fixture, DistributionGuestSendHostNames.First, Options(DistributionGuestSendHostNames.NoSuspend))).Equals(Term.A(DistributionGuestSendHostNames.NoSuspend)));
        Check((await fixture.Producer.InvokeAsync(async context => await Call(
            context,
            DistributionGuestSendHostNames.NoSuspendSend,
            fixture.Connection.Remote,
            Term.A(DistributionGuestSendHostNames.First)
        ))).Equals(Term.A(ErlangBooleanAtoms.False)));
        var entered = Signal();
        Task<Term> waiting = fixture.Producer.InvokeAsync(async context =>
        {
            ValueTask<Term> operation = Call(
                context,
                DistributionGuestSendHostNames.Send,
                fixture.Connection.Remote,
                Term.A(DistributionGuestSendHostNames.Second)
            );
            entered.TrySetResult();

            return await operation;
        });
        await entered.Task.WaitAsync(DistributionGuestSendHostNames.Deadline);
        Check(!waiting.IsCompleted);
        await fixture.Inner.DrainFiller(fixture.Connection, 1);
        Check((await waiting.WaitAsync(DistributionGuestSendHostNames.Deadline)).Equals(Term.A(DistributionGuestSendHostNames.Second)));
        await fixture.Inner.DrainFiller(fixture.Connection, capacity - 1);
        CheckSend(
            await fixture.Take(),
            fixture,
            DistributionGuestSendHostNames.Second,
            sender: false
        );
        fixture.CheckEmpty();
        Check((await Send(fixture, DistributionGuestSendHostNames.Third, Options(DistributionGuestSendHostNames.NoConnect))).Equals(Term.A(DistributionGuestSendHostNames.Ok)));
        CheckSend(
            await fixture.Take(),
            fixture,
            DistributionGuestSendHostNames.Third,
            sender: false
        );
        foreach (Term invalid in new Term[]
        {
            Options(DistributionGuestSendHostNames.NoConnect, DistributionGuestSendHostNames.InvalidOption),
            new Cons(Term.A(DistributionGuestSendHostNames.NoConnect), Term.A(DistributionGuestSendHostNames.InvalidOption))
        })
        {
            try
            {
                await Send(fixture, DistributionGuestSendHostNames.First, invalid);
                Check(false);
            }
            catch (ErlangException exception)
            {
                Check(exception.Reason.Equals(Term.A(DistributionGuestSendHostNames.BadArgument)));
            }
        }
        fixture.CheckEmpty();
        fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
        Check((await Send(fixture, DistributionGuestSendHostNames.First, Options(DistributionGuestSendHostNames.NoConnect))).Equals(Term.A(DistributionGuestSendHostNames.NoConnect)));
    }

    private static async Task TraceAdmission()
    {
        await using var fixture = await DistributionGuestSendHostFixture.Create(DistributionPassThroughFlags.SendSender);
        await ConfigureTrace(fixture, Term.I(DistributionGuestSendHostNames.Label));
        int capacity = fixture.Inner.Fill(fixture.Connection);
        Term before = await Serial(fixture);
        await Send(fixture, DistributionGuestSendHostNames.First, Options(DistributionGuestSendHostNames.NoSuspend));
        Check((await Serial(fixture)).Equals(before));
        await fixture.Inner.DrainFiller(fixture.Connection, capacity);
        await Send(fixture, DistributionGuestSendHostNames.Second, Options(DistributionGuestSendHostNames.NoSuspend));
        var packet = await fixture.Take();
        Check(packet.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.SendSenderTrace),
            fixture.Producer.Process.Pid,
            fixture.Connection.Remote,
            Term.Tuple(
                Term.I(1),
                Term.I(DistributionGuestSendHostNames.Label),
                Term.I(DistributionGuestSendHostNames.InitialSerial + 1),
                fixture.Producer.Process.Pid,
                Term.I(DistributionGuestSendHostNames.LastCount)
            )
        )));
        Check((await Serial(fixture)).Equals(Term.Tuple(
            Term.A(SequenceTraceNames.Serial),
            Term.Tuple(Term.I(DistributionGuestSendHostNames.LastCount), Term.I(DistributionGuestSendHostNames.InitialSerial + 1))
        )));
        fixture.CheckEmpty();
    }

    private static async Task TraceLabelProfiles()
    {
        foreach (ulong flags in new[] { 0UL, DistributionPassThroughFlags.BigSequenceTraceLabels | DistributionPassThroughFlags.AlternativeActionSignal })
        {
            await using var fixture = await DistributionGuestSendHostFixture.Create(flags);
            await ConfigureTrace(fixture, Term.A(DistributionGuestSendHostNames.First));
            await Send(fixture, DistributionGuestSendHostNames.Second, Options(DistributionGuestSendHostNames.Priority));
            var packet = await fixture.Take();
            if (flags == 0)
                CheckSend(
                    packet,
                    fixture,
                    DistributionGuestSendHostNames.Second,
                    sender: false
                );
            else
            {
                Check(packet.Control.Items.Count == DistributionAlternativePidSendLayout.TraceArity);
                Check(packet.Control.Items[0].Equals(Term.I(DistributionControlOpcodes.AlternativeActionSend)));
                Check(packet.Control.Items[1].Equals(Term.I(DistributionAlternativePidSendLayout.SupportedFlags)));
                Check(((TupleTerm)packet.Control.Items[DistributionAlternativePidSendLayout.TokenIndex]).Items[1].Equals(Term.A(DistributionGuestSendHostNames.First)));
            }
        }
    }

    private static async Task CancellationAndGeneration()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var connection = fixture.Connection;
        int capacity = fixture.Fill(connection);
        var entered = Signal();
        Task<Term> pending = StartBlocked(fixture, connection, entered);
        await entered.Task.WaitAsync(DistributionGuestSendHostNames.Deadline);
        Check(!pending.IsCompleted);
        await connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(DistributionGuestSendHostNames.Kill));

            return true;
        });
        try
        {
            await pending.WaitAsync(DistributionGuestSendHostNames.Deadline);
            Check(false);
        }
        catch (OperationCanceledException)
        {
        }
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGuestSendHostNames.Deadline);
        await fixture.DrainFiller(connection, capacity);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(connection.Host).QueuedPackets == 0);
        var successor = fixture.CreateActor();
        capacity = fixture.Fill(connection);
        entered = Signal();
        Task<Term> retired = successor.InvokeAsync(async context =>
        {
            ValueTask<Term> operation = Call(
                context,
                DistributionGuestSendHostNames.Send,
                connection.Remote,
                Term.A(DistributionGuestSendHostNames.First)
            );
            entered.TrySetResult();

            return await operation;
        });
        await entered.Task.WaitAsync(DistributionGuestSendHostNames.Deadline);
        Check(capacity > 0 && !retired.IsCompleted);
        fixture.Runtime.DistributionPending.CloseConnected(connection.Host);
        Check((await retired.WaitAsync(DistributionGuestSendHostNames.Deadline)).Equals(Term.A(DistributionGuestSendHostNames.First)));
        // Reuse the now-dead producer slot for a replacement controller through the existing fixture.
        var replacement = await fixture.CreateConnection(connection.Remote.Node);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(replacement.Host).QueuedPackets == 0);
        await successor.InvokeAsync(async context => await Call(
            context,
            DistributionGuestSendHostNames.Send,
            replacement.Remote,
            Term.A(DistributionGuestSendHostNames.Third)
        ));
        Check((await fixture.Take(replacement)).Payload!.Equals(Term.A(DistributionGuestSendHostNames.Third)));
    }

    private static async Task HaltOwnsBlockedSend()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Check(fixture.Fill(fixture.Connection) > 0);
        var entered = Signal();
        Task<Term> pending = StartBlocked(fixture, fixture.Connection, entered);
        await entered.Task.WaitAsync(DistributionGuestSendHostNames.Deadline);
        Check(!pending.IsCompleted);
        await fixture.Runtime.RequestImmediateHalt(DistributionGuestSendHostNames.HaltStatus).WaitAsync(DistributionGuestSendHostNames.Deadline);
        try
        {
            await pending.WaitAsync(DistributionGuestSendHostNames.Deadline);
            Check(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or ProcessHaltTransfer)
        {
        }
        await fixture.Runtime.HaltQuiescence.WaitAsync(DistributionGuestSendHostNames.Deadline);
    }

    private static async Task CodecRefusals()
    {
        await using var fixture = await DistributionGuestSendHostFixture.Create(DistributionPassThroughFlags.AlternativeActionSignal);
        var valid = new DistributionPassThroughMessage(
            Term.Tuple(
                Term.I(DistributionControlOpcodes.AlternativeActionSend),
                Term.I(DistributionAlternativePidSendLayout.Priority),
                fixture.Producer.Process.Pid,
                fixture.Connection.Remote
            ),
            Term.A(DistributionGuestSendHostNames.First)
        );
        byte[] wire = DistributionPassThroughCodec.Encode(valid, negotiatedFlags: fixture.Flags);
        try
        {
            DistributionPassThroughCodec.DecodeProcessPacket(wire, negotiatedFlags: fixture.Flags);
            Check(false);
        }
        catch (NotSupportedException)
        {
        }
        try
        {
            DistributionPassThroughCodec.Encode(valid);
            Check(false);
        }
        catch (NotSupportedException)
        {
        }
        foreach (int invalidFlags in new[] { -1, 4, DistributionAlternativePidSendLayout.Token })
        {
            try
            {
                DistributionPassThroughCodec.Encode(
                    new(
                        Term.Tuple(
                            Term.I(DistributionControlOpcodes.AlternativeActionSend),
                            Term.I(invalidFlags),
                            fixture.Producer.Process.Pid,
                            fixture.Connection.Remote
                        ),
                        valid.Payload
                    ),
                    negotiatedFlags: fixture.Flags
                );
                Check(false);
            }
            catch (ArgumentException)
            {
            }
        }
        fixture.CheckEmpty();
    }

    private static Task<Term> StartBlocked(DistributionLifecycleHostFixture fixture, DistributionLifecycleHostConnection connection, TaskCompletionSource entered)
        => fixture.Producer.InvokeAsync(async context =>
        {
            ValueTask<Term> operation = Call(
                context,
                DistributionGuestSendHostNames.Send,
                connection.Remote,
                Term.A(DistributionGuestSendHostNames.First)
            );
            entered.TrySetResult();

            return await operation;
        });

    private static Task<Term> Send(DistributionGuestSendHostFixture fixture, string marker, Term? options = null)
        => fixture.Producer.InvokeAsync(async context => options is null
            ? await Call(
                context,
                DistributionGuestSendHostNames.Send,
                fixture.Connection.Remote,
                Term.A(marker)
            )
            : await Call(
                context,
                DistributionGuestSendHostNames.Send,
                fixture.Connection.Remote,
                Term.A(marker),
                options
            ));

    private static Task<Term> Serial(DistributionGuestSendHostFixture fixture) => fixture.Producer.InvokeAsync(async context =>
        await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Serial)));

    private static Task<bool> ConfigureTrace(DistributionGuestSendHostFixture fixture, Term label)
        => fixture.Producer.InvokeAsync(async context =>
        {
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                label
            );
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(DistributionGuestSendHostNames.LastCount), Term.I(DistributionGuestSendHostNames.InitialSerial))
            );
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Send),
                Term.A(ErlangBooleanAtoms.True)
            );

            return true;
        });

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static Term Options(params string[] names) => Cons.From(names.Select(Term.A));

    private static void CheckSend(
        DistributionDecodedProcessPacket packet,
        DistributionGuestSendHostFixture fixture,
        string marker,
        bool sender
    )
    {
        Check(packet.Control.Equals(Term.Tuple(
            Term.I(sender ? DistributionControlOpcodes.SendSender : DistributionControlOpcodes.Send),
            sender ? fixture.Producer.Process.Pid : Term.A(DistributionPassThroughLayout.EmptyCookie),
            fixture.Connection.Remote
        )));
        Check(packet.Payload!.Equals(Term.A(marker)));
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionGuestSendHostNames.Failure);
    }
}
