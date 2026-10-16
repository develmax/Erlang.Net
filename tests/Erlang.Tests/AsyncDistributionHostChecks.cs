using Erlang;
using Erlang.Differential;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class AsyncDistributionHostChecks
{
    internal static async Task Run()
    {
        await FlagContracts();
        await AcceptedOrderSurvivesProducerDeath();
        await HardBoundAndHalt();
        await CancelBeforeAcceptance();
        await RetiredGenerationDoesNotReplay();
    }

    private static async Task FlagContracts()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        await fixture.Producer.InvokeAsync(async context =>
        {
            Check((await Flag(context, true)).Equals(Term.A(ErlangBooleanAtoms.False)));
            Check((await Flag(context, true)).Equals(Term.A(ErlangBooleanAtoms.True)));
            foreach (Term invalid in new Term[] { Term.I(1), Term.A(AsyncDistributionHostNames.Invalid) })
            {
                try
                {
                    await Call(
                        context,
                        AsyncDistributionHostNames.ProcessFlag,
                        Term.A(AsyncDistributionHostNames.AsyncDistribution),
                        invalid
                    );
                    Check(false);
                }
                catch (ErlangException failure)
                {
                    Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                }
            }
            Check((await Flag(context, false)).Equals(Term.A(ErlangBooleanAtoms.True)));
            Check((await Flag(context, false)).Equals(Term.A(ErlangBooleanAtoms.False)));
            Check((await Call(
                context,
                AsyncDistributionHostNames.ProcessFlag,
                Term.A(AsyncDistributionHostNames.TrapExit),
                Term.A(ErlangBooleanAtoms.True)
            )).Equals(Term.A(ErlangBooleanAtoms.False)));
            Check((await Call(
                context,
                AsyncDistributionHostNames.ProcessFlag,
                Term.A(AsyncDistributionHostNames.SaveCalls),
                Term.I(0)
            )).Equals(Term.I(0)));

            return true;
        });
        var other = fixture.CreateActor();
        Check((await other.InvokeAsync(async context => await Flag(context, false))).Equals(Term.A(ErlangBooleanAtoms.False)));
        await using var isolated = new ProcessRuntime();
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var independent = isolated.Spawn(async context =>
        {
            try
            {
                Term previous = await Flag(context, false);
                observed.TrySetResult(previous);

                return previous;
            }
            catch (Exception failure)
            {
                observed.TrySetException(failure);
                throw;
            }
        });
        try
        {
            Check((await observed.Task.WaitAsync(AsyncDistributionHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.False)));
        }
        finally
        {
            await independent.Completion.WaitAsync(AsyncDistributionHostNames.Deadline);
        }
    }

    private static async Task AcceptedOrderSurvivesProducerDeath()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        int capacity = fixture.Fill(fixture.Connection);
        Integer queuedBefore = await SendPending(fixture, fixture.Connection);
        var entered = Signal();
        Task<Term> normal = fixture.Producer.InvokeAsync(async context =>
        {
            var operation = Call(
                context,
                AsyncDistributionHostNames.Send,
                fixture.Connection.Remote,
                Term.A(AsyncDistributionHostNames.Normal)
            );
            entered.TrySetResult();

            return await operation;
        });
        await entered.Task.WaitAsync(AsyncDistributionHostNames.Deadline);
        Check(!normal.IsCompleted);
        var asynchronous = fixture.CreateActor();
        await asynchronous.InvokeAsync(async context =>
        {
            await Flag(context, true);
            Check((await Call(
                context,
                AsyncDistributionHostNames.Send,
                fixture.Connection.Remote,
                Term.A(AsyncDistributionHostNames.First)
            )).Equals(Term.A(AsyncDistributionHostNames.First)));
            Check((await Call(
                context,
                AsyncDistributionHostNames.NoSuspend,
                fixture.Connection.Remote,
                Term.A(AsyncDistributionHostNames.Second),
                Term.List(Term.A(AsyncDistributionHostNames.NoConnect))
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            await Flag(context, false);

            return true;
        }).WaitAsync(AsyncDistributionHostNames.Deadline);
        Check(!normal.IsCompleted);
        Integer acceptedPending = await SendPending(fixture, fixture.Connection);
        Check(acceptedPending.Value > queuedBefore.Value);
        await fixture.Connection.Controller.Invoke(context =>
        {
            fixture.Runtime.Exit(context, asynchronous.Process.Pid, Term.A(AsyncDistributionHostNames.Kill));

            return true;
        });
        await asynchronous.Process.Completion.WaitAsync(AsyncDistributionHostNames.Deadline);
        Check((await SendPending(fixture, fixture.Connection)).Equals(acceptedPending));
        await fixture.DrainFiller(fixture.Connection, 1);
        Check((await normal.WaitAsync(AsyncDistributionHostNames.Deadline)).Equals(Term.A(AsyncDistributionHostNames.Normal)));
        await fixture.DrainFiller(fixture.Connection, capacity - 1);
        await Drain(fixture.Runtime);
        Integer transferredPending = await SendPending(fixture, fixture.Connection);
        long consumed = 0;
        foreach (string expected in new[] { AsyncDistributionHostNames.Normal, AsyncDistributionHostNames.First, AsyncDistributionHostNames.Second })
        {
            HostDistributionBuffer actualBuffer = await TakeRaw(fixture, fixture.Connection);
            var actual = DistributionPassThroughCodec.DecodeProcessPacket(actualBuffer.Segments[0].ToArray());
            Check(actual.Payload!.Equals(Term.A(expected)));
            long credit = (long)typeof(HostDistributionBuffer).GetProperty(AsyncDistributionHostNames.AccountedBytes,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(actualBuffer)!;
            consumed = checked(consumed + credit);
            Check((await SendPending(fixture, fixture.Connection)).Value == transferredPending.Value - consumed);
        }
        Check(transferredPending.Value == consumed);
        await Drain(fixture.Runtime);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
        Check((await SendPending(fixture, fixture.Connection)).Value == 0);
    }

    private static async Task HardBoundAndHalt()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Check(fixture.Fill(fixture.Connection) > 0);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await Flag(context, true);
            await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Send),
                Term.A(ErlangBooleanAtoms.True)
            );
            for (int index = 0; index < AsyncDistributionHostParameters.MaximumStagedPackets; index++)
                Check((await Call(
                    context,
                    AsyncDistributionHostNames.Send,
                    fixture.Connection.Remote,
                    Term.A(AsyncDistributionHostNames.First)
                )).Equals(Term.A(AsyncDistributionHostNames.First)));
            Term beforeRefusal = await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.Info,
                Term.A(SequenceTraceNames.Serial)
            );
            try
            {
                await Call(
                    context,
                    AsyncDistributionHostNames.NoSuspend,
                    fixture.Connection.Remote,
                    Term.A(AsyncDistributionHostNames.Second)
                );
                Check(false);
            }
            catch (ErlangException failure)
            {
                Check(failure.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)));
            }
            Term afterRefusal = await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.Info,
                Term.A(SequenceTraceNames.Serial)
            );
            Check(beforeRefusal.Equals(afterRefusal));

            return true;
        }).WaitAsync(AsyncDistributionHostNames.Deadline);
        object pendingBeforeHalt = fixture.Runtime.DistributionPending;
        await fixture.Runtime.RequestImmediateHalt(AsyncDistributionHostParameters.HaltStatus).WaitAsync(AsyncDistributionHostNames.Deadline);
        await fixture.Runtime.HaltQuiescence.WaitAsync(AsyncDistributionHostNames.Deadline);
        await Drain(pendingBeforeHalt);
    }

    private static async Task CancelBeforeAcceptance()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(AsyncDistributionHostNames.ClosureSource);
        compiled.Definition.Register(fixture.Runtime.Modules);
        FunctionTerm original = (FunctionTerm)await fixture.Producer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context, AsyncDistributionHostNames.ClosureModule, AsyncDistributionHostNames.ClosureFunction));
        var identity = original.LocalIdentity!;
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        object runtimeGate = typeof(ProcessRuntime).GetField(AsyncDistributionHostNames.Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Runtime)!;
        var payload = new FunctionTerm(
            () =>
        {
            Check(!Monitor.IsEntered(runtimeGate));
            entered.TrySetResult();
            Check(release.Wait(AsyncDistributionHostNames.Deadline));

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
            await fixture.Producer.InvokeAsync(async context => await Flag(context, true));
            operation = fixture.Producer.InvokeAsync(async context => await Call(
                context,
                AsyncDistributionHostNames.Send,
                fixture.Connection.Remote,
                payload
            ));
            await entered.Task.WaitAsync(AsyncDistributionHostNames.Deadline);
            await fixture.Connection.Controller.Invoke(context =>
            {
                fixture.Runtime.Exit(context, fixture.Producer.Process.Pid, Term.A(AsyncDistributionHostNames.Kill));

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
                    await operation.WaitAsync(AsyncDistributionHostNames.Deadline);
                    Check(false);
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
        await fixture.Producer.Process.Completion.WaitAsync(AsyncDistributionHostNames.Deadline);
        await Drain(fixture.Runtime);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
    }

    private static async Task RetiredGenerationDoesNotReplay()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Check(fixture.Fill(fixture.Connection) > 0);
        await fixture.Producer.InvokeAsync(async context =>
        {
            await Flag(context, true);
            await Call(
                context,
                AsyncDistributionHostNames.Send,
                fixture.Connection.Remote,
                Term.A(AsyncDistributionHostNames.First)
            );

            return true;
        });
        fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
        await Drain(fixture.Runtime);
        var replacement = await fixture.CreateConnection(fixture.Connection.Remote.Node);
        Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(replacement.Host).QueuedPackets == 0);
        Check((await SendPending(fixture, replacement)).Value == 0);
        await fixture.Producer.InvokeAsync(async context => await Call(
            context,
            AsyncDistributionHostNames.Send,
            replacement.Remote,
            Term.A(AsyncDistributionHostNames.Second)
        ));
        Check((await fixture.Take(replacement)).Payload!.Equals(Term.A(AsyncDistributionHostNames.Second)));
        Check((await SendPending(fixture, replacement)).Value == 0);
        await Drain(fixture.Runtime);
    }

    private static async Task<HostDistributionBuffer> TakeRaw(DistributionLifecycleHostFixture fixture, DistributionLifecycleHostConnection connection)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < AsyncDistributionHostNames.Deadline)
        {
            var packet = await connection.Controller.Invoke(context => fixture.Runtime.DistributionPending.TakeConnectedOutput(connection.Host, context, out _));
            if (packet is not null)
                return packet;
            await Task.Yield();
        }

        throw new TimeoutException(AsyncDistributionHostNames.Failure);
    }

    private static Task<Integer> SendPending(DistributionLifecycleHostFixture fixture, DistributionLifecycleHostConnection connection)
        => connection.Controller.InvokeAsync(async context =>
        {
            var statistics = (TupleTerm)await Call(context, AsyncDistributionHostNames.Statistics, connection.Host);

            return (Integer)statistics.Items[AsyncDistributionHostParameters.SendPendingIndex];
        });

    private static Task Drain(ProcessRuntime runtime) => Drain((object)runtime.DistributionPending);

    private static async Task Drain(object pending)
    {
        MethodInfo method = pending.GetType().GetMethod(AsyncDistributionHostNames.Drain, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(pending, null)!).WaitAsync(AsyncDistributionHostNames.Deadline);
    }

    private static ValueTask<Term> Flag(ProcessContext context, bool enabled) => Call(
        context,
        AsyncDistributionHostNames.ProcessFlag,
        Term.A(AsyncDistributionHostNames.AsyncDistribution),
        Term.A(enabled ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
    );

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(AsyncDistributionHostNames.Failure);
    }
}
