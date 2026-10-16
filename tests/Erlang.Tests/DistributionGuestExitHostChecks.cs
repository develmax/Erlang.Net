using Erlang;
using Erlang.Differential;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ExitApi = Erlang.Tests.DistributionGuestExitPublicRouteHostChecks;

namespace Erlang.Tests;

internal static class DistributionGuestExitHostChecks
{
    internal static async Task Run()
    {
        await ProfilesAndPublicForms();
        await FullOptionValidation();
        await TokenAndTracingUnchanged();
        await PublicCrossApiOrder();
        await NormalPressureCancellationAndRetirement();
        await AsyncAcceptedSurvivesProducer();
        await AsyncHardcapAndHalt();
        await SerializeOutsideGateAndCancel();
        await CodecBoundaries();
    }

    private static async Task ProfilesAndPublicForms()
    {
        foreach (ulong flags in new[] { 0UL, DistributionPassThroughFlags.ExitPayload,
            DistributionPassThroughFlags.AlternativeActionSignal,
            DistributionPassThroughFlags.AlternativeActionSignal | DistributionPassThroughFlags.ExitPayload })
        {
            await using var fixture = await DistributionGuestSendHostFixture.Create(flags);
            Term reason = Term.Tuple(Term.A(DistributionGuestExitHostNames.Reason), new BitString(new byte[] { 1, 2, 3 }));
            foreach (string function in new[] { ErlangModuleNames.Exit, DistributionGuestExitHostNames.ExitSignal })
            {
                foreach (bool priority in new[] { false, true })
                {
                    Term? options = priority ? Term.List(Term.A(DistributionGuestExitHostNames.Priority),
                        Term.A(DistributionGuestExitHostNames.Priority)) : null;
                    await Send(
                        fixture,
                        function,
                        reason,
                        options
                    );
                    var packet = await Take(fixture);
                    bool alternative = priority && (flags & DistributionPassThroughFlags.AlternativeActionSignal) != 0;
                    int opcode = alternative ? DistributionControlOpcodes.AlternativeActionSend :
                        (flags & DistributionPassThroughFlags.ExitPayload) != 0 ? DistributionControlOpcodes.PayloadExit2 : DistributionControlOpcodes.Exit2;
                    ExitApi.Check(packet.Control.Items[0].Equals(Term.I(opcode)) && Reason(packet).Equals(reason));
                    if (alternative)
                        ExitApi.Check(packet.Control.Items.Count == DistributionExplicitPidExitLayout.Arity &&
                            packet.Control.Items[DistributionExplicitPidExitLayout.FlagsIndex].Equals(Term.I(DistributionExplicitPidExitLayout.PriorityExitAction)));
                    fixture.CheckEmpty();
                }
            }
            foreach (string reasonName in new[] { ProcessExitSignals.Kill, ProcessExitReasons.Normal })
            {
                await Send(fixture, ErlangModuleNames.Exit, Term.A(reasonName));
                ExitApi.Check(Reason(await Take(fixture)).Equals(Term.A(reasonName)));
            }
            using var compiled = GeneratedModuleCompiler.Compile(DistributionGuestExitHostNames.GeneratedSource);
            compiled.Definition.Register(fixture.Runtime.Modules);
            await fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
                context,
                DistributionGuestExitHostNames.GeneratedModule,
                DistributionGuestExitHostNames.GeneratedFunction,
                fixture.Connection.Remote,
                reason
            ));
            ExitApi.Check(Reason(await Take(fixture)).Equals(reason));
            fixture.CheckEmpty();
        }
    }

    private static async Task FullOptionValidation()
    {
        await using var fixture = await DistributionGuestSendHostFixture.Create(DistributionPassThroughFlags.AlternativeActionSignal);
        await ConfigureToken(fixture);
        Term before = await Token(fixture);
        foreach (Term invalid in new Term[]
        {
            Term.List(Term.A(DistributionGuestExitHostNames.Priority), Term.A(DistributionGuestExitHostNames.InvalidOption)),
            new Cons(Term.A(DistributionGuestExitHostNames.Priority), Term.A(DistributionGuestExitHostNames.InvalidOption))
        })
        {
            foreach (string function in new[] { ErlangModuleNames.Exit, DistributionGuestExitHostNames.ExitSignal })
            {
                try
                {
                    await Send(
                        fixture,
                        function,
                        Term.A(DistributionGuestExitHostNames.Reason),
                        invalid
                    );
                    ExitApi.Check(false);
                }
                catch (ErlangException failure)
                {
                    ExitApi.Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                }
                ExitApi.Check((await Token(fixture)).Equals(before));
                fixture.CheckEmpty();
            }
        }
        await Send(
            fixture,
            ErlangModuleNames.Exit,
            Term.A(DistributionGuestExitHostNames.Reason),
            Term.List(Term.A(DistributionGuestExitHostNames.Priority), Term.A(DistributionGuestExitHostNames.Priority))
        );
        ExitApi.Check((await Take(fixture)).Control.Items[DistributionExplicitPidExitLayout.FlagsIndex]
            .Equals(Term.I(DistributionExplicitPidExitLayout.PriorityExitAction)));
        fixture.CheckEmpty();
        ExitApi.Check((await Token(fixture)).Equals(before));
    }

    private static async Task TokenAndTracingUnchanged()
    {
        await using var fixture = await DistributionGuestSendHostFixture.Create(DistributionPassThroughFlags.ExitPayload |
            DistributionPassThroughFlags.AlternativeActionSignal | DistributionPassThroughFlags.BigSequenceTraceLabels);
        var tracer = fixture.Inner.Connection.Controller;
        await ConfigureToken(fixture);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.SystemFlag,
                Term.A(SequenceTraceNames.SystemTracer),
                tracer.Process.Pid
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessTraceNames.Configure,
                context.Self,
                Term.A(ErlangBooleanAtoms.True),
                Term.List(Term.A(ProcessTraceNames.Send),
                    Term.Tuple(Term.A(ProcessTraceNames.Tracer), tracer.Process.Pid))
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestExitHostNames.ProcessFlag,
                Term.A(ProcessSavedCallNames.Flag),
                Term.I(32)
            );

            return true;
        });
        Term before = await Token(fixture);
        await Send(fixture, ErlangModuleNames.Exit, Term.A(DistributionGuestExitHostNames.Reason));
        await Take(fixture);
        await Send(
            fixture,
            DistributionGuestExitHostNames.ExitSignal,
            Term.A(DistributionGuestExitHostNames.Reason),
            Term.List(Term.A(DistributionGuestExitHostNames.Priority))
        );
        var prioritized = await Take(fixture);
        ExitApi.Check(prioritized.Control.Items.Count == DistributionExplicitPidExitLayout.Arity);
        ExitApi.Check((await Token(fixture)).Equals(before));
        ExitApi.Check(await tracer.InvokeAsync(async context => await context.ReceiveAsync<Term>(message => message, TimeSpan.Zero)) is null);
        await fixture.Producer.InvokeAsync(async context =>
        {
            var last = (TupleTerm)await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessInfoNames.Function,
                context.Self,
                Term.A(ProcessInfoNames.LastCalls)
            );
            ExitApi.Check(Cons.Items(last.Items[1]).All(item => !item.Equals(Term.A(ProcessSavedCallNames.Send))));

            return true;
        });
    }

    private static async Task PublicCrossApiOrder()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await fixture.Producer.InvokeAsync(async context =>
        {
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestExitHostNames.GroupLeader,
                context.Self,
                fixture.Connection.Remote
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Link,
                fixture.Connection.Remote
            );
            Term reason = Term.A(DistributionGuestExitHostNames.Reason);
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionGuestExitHostNames.SendFunction,
                fixture.Connection.Remote,
                reason
            );
            await ExitApi.Call(
                context,
                ErlangModuleNames.Exit,
                fixture.Connection.Remote,
                reason
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Unlink,
                fixture.Connection.Remote
            );

            return true;
        });
        foreach (int opcode in new[] { DistributionControlOpcodes.GroupLeader, DistributionControlOpcodes.Link,
            DistributionControlOpcodes.Send, DistributionControlOpcodes.Exit2, DistributionControlOpcodes.UnlinkId })
            ExitApi.Check((await fixture.Take(fixture.Connection)).Control.Items[0].Equals(Term.I(opcode)));
        ExitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
    }

    private static async Task NormalPressureCancellationAndRetirement()
    {
        await using var fixture = await DistributionGuestSendHostFixture.Create();
        int capacity = fixture.Inner.Fill(fixture.Connection);
        var entered = Signal();
        Task<Term> blocked = Start(fixture, entered);
        await entered.Task.WaitAsync(DistributionGuestExitHostNames.Deadline);
        ExitApi.Check(!blocked.IsCompleted);
        await fixture.Connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(ProcessExitSignals.Kill));

            return true;
        });
        try
        {
            await blocked.WaitAsync(DistributionGuestExitHostNames.Deadline);
            ExitApi.Check(false);
        }
        catch (OperationCanceledException)
        {
        }
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGuestExitHostNames.Deadline);
        await fixture.Inner.DrainFiller(fixture.Connection, capacity);
        fixture.CheckEmpty();
        // This fixture has no free actor slot; retire the now-empty old connection and use a fresh independent fixture.
        await using var retiredFixture = await DistributionLifecycleHostFixture.Create();
        ExitApi.Check(retiredFixture.Fill(retiredFixture.Connection) > 0);
        entered = Signal();
        Task<Term> retiring = retiredFixture.Producer.InvokeAsync(async context =>
        {
            var operation = ExitApi.Call(
                context,
                ErlangModuleNames.Exit,
                retiredFixture.Connection.Remote,
                Term.A(DistributionGuestExitHostNames.Reason)
            );
            entered.TrySetResult();

            return await operation;
        });
        await entered.Task.WaitAsync(DistributionGuestExitHostNames.Deadline);
        ExitApi.Check(!retiring.IsCompleted);
        retiredFixture.Runtime.DistributionPending.CloseConnected(retiredFixture.Connection.Host);
        ExitApi.Check((await retiring.WaitAsync(DistributionGuestExitHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.True)));
        var replacement = await retiredFixture.CreateConnection(retiredFixture.Connection.Remote.Node);
        ExitApi.Check(retiredFixture.Runtime.DistributionPending.ConnectedSnapshot(replacement.Host).QueuedPackets == 0);
    }

    private static async Task AsyncAcceptedSurvivesProducer()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        int capacity = fixture.Fill(fixture.Connection);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await Flag(context);
            await ExitApi.Call(
                context,
                ErlangModuleNames.Exit,
                fixture.Connection.Remote,
                Term.A(DistributionGuestExitHostNames.Reason)
            );

            return true;
        }).WaitAsync(DistributionGuestExitHostNames.Deadline);
        await fixture.Connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(ProcessExitSignals.Kill));

            return true;
        });
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGuestExitHostNames.Deadline);
        await fixture.DrainFiller(fixture.Connection, capacity);
        var packet = await fixture.Take(fixture.Connection);
        ExitApi.Check(packet.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit2),
            fixture.Producer.Process.Pid,
            fixture.Connection.Remote,
            Term.A(DistributionGuestExitHostNames.Reason)
        )));
        await Drain(fixture.Runtime);
        ExitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
    }

    private static async Task AsyncHardcapAndHalt()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        ExitApi.Check(fixture.Fill(fixture.Connection) > 0);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await Flag(context);
            for (int index = 0; index < DistributionGuestExitHostNames.MaximumStagedPackets; index++)
                await ExitApi.Call(
                    context,
                    ErlangModuleNames.Exit,
                    fixture.Connection.Remote,
                    Term.A(DistributionGuestExitHostNames.Reason)
                );
            try
            {
                await ExitApi.Call(
                    context,
                    ErlangModuleNames.Exit,
                    fixture.Connection.Remote,
                    Term.A(DistributionGuestExitHostNames.Reason)
                );
                ExitApi.Check(false);
            }
            catch (ErlangException failure)
            {
                ExitApi.Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)));
            }

            return true;
        }).WaitAsync(DistributionGuestExitHostNames.Deadline);
        object pending = fixture.Runtime.DistributionPending;
        await fixture.Runtime.RequestImmediateHalt(DistributionGuestExitHostNames.HaltStatus).WaitAsync(DistributionGuestExitHostNames.Deadline);
        await fixture.Runtime.HaltQuiescence.WaitAsync(DistributionGuestExitHostNames.Deadline);
        await DrainPending(pending);
    }

    private static async Task SerializeOutsideGateAndCancel()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionGuestExitHostNames.ClosureSource);
        compiled.Definition.Register(fixture.Runtime.Modules);
        var original = (FunctionTerm)await fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context, DistributionGuestExitHostNames.ClosureModule, DistributionGuestExitHostNames.ClosureFunction));
        var identity = original.LocalIdentity!;
        object gate = typeof(ProcessRuntime).GetField(DistributionGuestExitHostNames.GateMember,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Runtime)!;
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        var reason = new FunctionTerm(
            () =>
        {
            ExitApi.Check(!Monitor.IsEntered(gate));
            entered.TrySetResult();
            ExitApi.Check(release.Wait(DistributionGuestExitHostNames.Deadline));

            return identity;
        },
            original.Arity,
            original.Environment,
            original.Invoke
        );
        Task<Term>? operation = null;
        Exception? primary = null;
        bool killed = false;
        try
        {
            operation = fixture.Producer.InvokeAsync(async context => await ExitApi.Call(
                context,
                ErlangModuleNames.Exit,
                fixture.Connection.Remote,
                reason
            ));
            await entered.Task.WaitAsync(DistributionGuestExitHostNames.Deadline);
            await fixture.Connection.Controller.Invoke(context =>
            {
                fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(ProcessExitSignals.Kill));

                return true;
            });
            killed = true;
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        finally
        {
            release.Set();
            if (operation is not null)
            {
                try
                {
                    await operation.WaitAsync(DistributionGuestExitHostNames.Deadline);
                    ExitApi.Check(false);
                }
                catch (OperationCanceledException) when (killed)
                {
                }
                catch (Exception settlement)
                {
                    primary = primary is null ? settlement : new AggregateException(primary, settlement);
                }
            }
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
        await fixture.Producer.Process.Completion.WaitAsync(DistributionGuestExitHostNames.Deadline);
        await Drain(fixture.Runtime);
        ExitApi.Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
    }

    private static async Task CodecBoundaries()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Term reason = Term.A(DistributionGuestExitHostNames.Reason);
        byte[] packet = DistributionPassThroughCodec.EncodeExplicitPidExit(
            fixture.Producer.Process.Pid,
            fixture.Connection.Remote,
            reason,
            true,
            DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.AlternativeActionSignal
        );
        try
        {
            DistributionPassThroughCodec.DecodeProcessPacket(packet,
                negotiatedFlags: DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.AlternativeActionSignal);
            ExitApi.Check(false);
        }
        catch (NotSupportedException)
        {
        }
        try
        {
            DistributionPassThroughCodec.EncodeExplicitPidExit(
                fixture.Producer.Process.Pid,
                fixture.Connection.Remote,
                reason,
                false,
                maximumBytes: DistributionGuestExitHostNames.SmallPacketLimit
            );
            ExitApi.Check(false);
        }
        catch (ErlangException failure)
        {
            ExitApi.Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)));
        }
        foreach (int invalid in new[] { DistributionExplicitPidExitLayout.ExitAction,
            DistributionExplicitPidExitLayout.PriorityExitAction | DistributionAlternativePidSendLayout.Token,
            DistributionExplicitPidExitLayout.PriorityExitAction | DistributionGuestExitHostNames.UnsupportedAliasFlag })
        {
            var control = Term.Tuple(
                Term.I(DistributionControlOpcodes.AlternativeActionSend),
                Term.I(invalid),
                fixture.Producer.Process.Pid,
                fixture.Connection.Remote
            );
            byte[] raw = new byte[] { DistributionPassThroughLayout.Marker }.Concat(ExternalTermFormat.Encode(control))
                .Concat(ExternalTermFormat.Encode(reason)).ToArray();
            try
            {
                DistributionPassThroughCodec.DecodeExplicitPidExit(raw,
                    DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.AlternativeActionSignal);
                ExitApi.Check(false);
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static Task<Term> Send(
        DistributionGuestSendHostFixture fixture,
        string function,
        Term reason,
        Term? options = null
    )
        => fixture.Producer.InvokeAsync(async context =>
        {
            Term result = await ExitApi.Call(
                context,
                function,
                fixture.Connection.Remote,
                reason,
                options
            );
            ExitApi.Check(result.Equals(Term.A(ErlangBooleanAtoms.True)));

            return result;
        });

    private static Task<Term> Start(DistributionGuestSendHostFixture fixture, TaskCompletionSource entered)
        => fixture.Producer.InvokeAsync(async context =>
        {
            var operation = ExitApi.Call(
                context,
                ErlangModuleNames.Exit,
                fixture.Connection.Remote,
                Term.A(DistributionGuestExitHostNames.Reason)
            );
            entered.TrySetResult();

            return await operation;
        });

    private static async Task<DistributionDecodedProcessPacket> Take(DistributionGuestSendHostFixture fixture)
    {
        var buffer = await fixture.Connection.Controller.Invoke(context => fixture.Runtime.DistributionPending.TakeConnectedOutput(
            fixture.Connection.Host, context, out _));
        ExitApi.Check(buffer is { SegmentCount: 1 });

        return DistributionPassThroughCodec.DecodeExplicitPidExit(buffer!.Segments[0].ToArray(), fixture.Flags);
    }

    private static Term Reason(DistributionDecodedProcessPacket packet)
        => packet.Payload ?? packet.Control.Items[DistributionControlOpcodes.ExitReasonIndex];

    private static Task<bool> ConfigureToken(DistributionGuestSendHostFixture fixture)
        => fixture.Producer.InvokeAsync(async context =>
        {
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                Term.I(DistributionGuestExitHostNames.Label)
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(DistributionGuestExitHostNames.LastCount),
                    Term.I(DistributionGuestExitHostNames.InitialSerial))
            );
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Send),
                Term.A(ErlangBooleanAtoms.True)
            );

            return true;
        });

    private static Task<Term> Token(DistributionGuestSendHostFixture fixture)
        => fixture.Producer.Invoke(context =>
        {
            object state = typeof(ProcessContext).GetProperty(DistributionGuestExitHostNames.SequentialTraceMember,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(context)!;
            object token = state.GetType().GetProperty(DistributionGuestExitHostNames.TokenMember)!.GetValue(state)!;

            return (Term)token.GetType().GetMethod(DistributionGuestExitHostNames.TokenToTerm)!.Invoke(token, null)!;
        });

    private static ValueTask<Term> Flag(ProcessContext context)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionGuestExitHostNames.ProcessFlag,
            Term.A(DistributionGuestExitHostNames.AsyncDistribution),
            Term.A(ErlangBooleanAtoms.True)
        );

    private static Task Drain(ProcessRuntime runtime) => DrainPending(runtime.DistributionPending);

    private static async Task DrainPending(object pending)
    {
        var method = pending.GetType().GetMethod(DistributionGuestExitHostNames.DrainMember, BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(pending, null)!).WaitAsync(DistributionGuestExitHostNames.Deadline);
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
