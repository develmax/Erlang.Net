using Erlang;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Erlang.Tests;

internal static class DistributionPublicationLifecycleHostChecks
{
    internal static async Task Run()
    {
        await MonitorAndUnlinkCommit();
        await MultipleChannelsAndPublicSlot();
        await ThrowingCancellationCallback();
        await HaltOwnsPendingOutput();
    }

    private static async Task MonitorAndUnlinkCommit()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var runtime = fixture.Runtime;
        var connection = fixture.Connection;
        int capacity = fixture.Fill(connection);
        Check(capacity > 0);
        var reserved = Signal();
        Task<ReferenceTerm> monitoring = fixture.Producer.InvokeAsync(async context =>
        {
            var operation = runtime.DistributionPending.BeginConnectedMonitorSignalAsync(connection.Host, context, connection.Remote);
            reserved.TrySetResult();

            return await operation;
        });
        await reserved.Task.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(!monitoring.IsCompleted && MonitorCount(runtime) == 0);
        await fixture.DrainFiller(connection, 1);
        ReferenceTerm reference = await monitoring.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(MonitorCount(runtime) == 1);
        await fixture.DrainFiller(connection, capacity - 1);
        Check((await fixture.Take(connection)).Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Monitor),
            fixture.Producer.Process.Pid,
            connection.Remote,
            reference
        )));
        await fixture.Refill(connection, capacity);
        reserved = Signal();
        Task<bool> removing = fixture.Producer.InvokeAsync(async context =>
        {
            var operation = runtime.DistributionPending.EndConnectedMonitorSignalAsync(context, reference);
            reserved.TrySetResult();

            return await operation;
        });
        await reserved.Task.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(!removing.IsCompleted && MonitorCount(runtime) == 1);
        await fixture.DrainFiller(connection, 1);
        Check(await removing.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline));
        Check(MonitorCount(runtime) == 0);
        await fixture.DrainFiller(connection, capacity - 1);
        Check((await fixture.Take(connection)).Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Demonitor),
            fixture.Producer.Process.Pid,
            connection.Remote,
            reference
        )));
        await connection.Controller.Invoke(context =>
        {
            runtime.DistributionPending.ApplyConnectedLinkSignal(
                connection.Host,
                context,
                connection.Remote,
                fixture.Producer.Process.Pid
            );

            return true;
        });
        await fixture.Producer.InvokeAsync(async context => await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero));
        var producer = await fixture.Producer.Ready;
        Check(HasLink(runtime, producer, connection.Remote));
        await fixture.Refill(connection, capacity);
        reserved = Signal();
        Task<ulong?> unlinking = fixture.Producer.InvokeAsync(async context =>
        {
            var operation = runtime.DistributionPending.BeginConnectedUnlinkSignalAsync(connection.Host, context, connection.Remote);
            reserved.TrySetResult();

            return await operation;
        });
        await reserved.Task.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(!unlinking.IsCompleted && HasLink(runtime, producer, connection.Remote));
        await fixture.DrainFiller(connection, 1);
        ulong? unlinkId = await unlinking.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(unlinkId.HasValue && !HasLink(runtime, producer, connection.Remote));
        await fixture.DrainFiller(connection, capacity - 1);
        Check((await fixture.Take(connection)).Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.UnlinkId),
            new Integer(unlinkId!.Value),
            fixture.Producer.Process.Pid,
            connection.Remote
        )));
    }

    private static async Task MultipleChannelsAndPublicSlot()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var second = await fixture.CreateConnection(DistributionPublicationLifecycleHostNames.SecondRemote);
        foreach (var connection in new[] { fixture.Connection, second })
        {
            await connection.Controller.Invoke(context =>
            {
                fixture.Runtime.DistributionPending.ApplyConnectedLinkSignal(
                    connection.Host,
                    context,
                    connection.Remote,
                    fixture.Producer.Process.Pid
                );

                return true;
            });
        }
        await fixture.Producer.InvokeAsync(async context => await context.ReceiveAsync<Term>(_ => null, TimeSpan.Zero));
        int firstCapacity = fixture.Fill(fixture.Connection);
        int secondCapacity = fixture.Fill(second);
        Check(firstCapacity > 0 && secondCapacity > 0);
        Task ending = fixture.Producer.DisposeAsync().AsTask();
        var deadline = Stopwatch.StartNew();
        while (fixture.Runtime.IsAlive(fixture.Producer.Process.Pid))
        {
            Check(deadline.Elapsed < DistributionPublicationLifecycleHostNames.Deadline);
            await Task.Yield();
        }
        Check(!ending.IsCompleted && !fixture.Producer.Process.Completion.IsCompleted);
        // The initializer and four actors occupied all five public process slots. Public FREE must release a slot while terminal work remains owned.
        var replacement = fixture.CreateActor();
        Check(fixture.Runtime.IsAlive(replacement.Process.Pid) && !replacement.Process.Pid.Equals(fixture.Producer.Process.Pid));
        await fixture.DrainFiller(fixture.Connection, firstCapacity);
        await RequireTerminal(fixture, fixture.Connection);
        Check(!ending.IsCompleted && fixture.Runtime.DistributionPending.ConnectedSnapshot(second.Host).QueuedPackets == secondCapacity);
        await fixture.DrainFiller(second, secondCapacity);
        await RequireTerminal(fixture, second);
        await ending.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check((await fixture.Producer.Process.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static async Task ThrowingCancellationCallback()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        var observer = fixture.CreateActor();
        await observer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionPublicationLifecycleHostNames.ProcessFlag,
            Term.A(DistributionPublicationLifecycleHostNames.TrapExit),
            Term.A(ErlangBooleanAtoms.True)
        ));
        var producer = await fixture.Producer.Ready;
        await fixture.Producer.Invoke(context =>
        {
            fixture.Runtime.Register(DistributionPublicationLifecycleHostNames.Registered, context.Self);
            fixture.Runtime.Link(context, observer.Process.Pid);
            _ = context.Cancellation.Register(() => throw new InvalidOperationException(DistributionPublicationLifecycleHostNames.InjectedFailure));

            return true;
        });
        ReferenceTerm monitor = await observer.Invoke(context => fixture.Runtime.Monitor(context, fixture.Producer.Process.Pid));
        fixture.ExpectCleanupFailure = true;
        try
        {
            await fixture.Producer.DisposeAsync().AsTask().WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
            Check(false);
        }
        catch (AggregateException exception)
        {
            Check(exception.Flatten().InnerExceptions.All(error => error is InvalidOperationException &&
                error.Message == DistributionPublicationLifecycleHostNames.InjectedFailure));
        }
        var unwound = (TaskCompletionSource)typeof(ProcessContext).GetProperty(
            DistributionPublicationLifecycleHostNames.UnwoundMember,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!.GetValue(producer)!;
        await unwound.Task.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(!fixture.Runtime.IsAlive(producer.Self));
        Check(fixture.Runtime.WhereIs(DistributionPublicationLifecycleHostNames.Registered).Equals(Term.A(DistributionPublicationLifecycleHostNames.Undefined)));
        Term? down = await observer.InvokeAsync(async context => await context.ReceiveAsync(
            term => term.Equals(Term.Tuple(
                Term.A(ProcessMessageTags.MonitorDown),
                monitor,
                Term.A(ProcessMonitorKinds.Process),
                producer.Self,
                Term.A(ProcessExitReasons.Normal)
            )) ? term : null,
            DistributionPublicationLifecycleHostNames.Deadline
        ));
        Check(down is not null);
        Term? exit = await observer.InvokeAsync(async context => await context.ReceiveAsync(
            term => term.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), producer.Self, Term.A(ProcessExitReasons.Normal))) ? term : null,
            DistributionPublicationLifecycleHostNames.Deadline
        ));
        Check(exit is not null && !HasLink(fixture.Runtime, await observer.Ready, producer.Self));
    }

    private static async Task HaltOwnsPendingOutput()
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        Check(fixture.Fill(fixture.Connection) > 0);
        var reserved = Signal();
        Task<bool> sending = fixture.Producer.InvokeAsync(async context =>
        {
            var operation = fixture.Runtime.DistributionPending.EnqueueConnectedOutputAsync(
                fixture.Connection.Host,
                context,
                DistributionPassThroughCodec.Send(fixture.Connection.Remote, Term.A(DistributionPublicationLifecycleHostNames.Filler))
            );
            reserved.TrySetResult();
            await operation;

            return true;
        });
        await reserved.Task.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(!sending.IsCompleted);
        var halt = await fixture.Runtime.RequestImmediateHalt(DistributionPublicationLifecycleHostNames.HaltStatus)
            .WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        Check(halt.Status == DistributionPublicationLifecycleHostNames.HaltStatus);
        try
        {
            await sending.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
            Check(false);
        }
        catch (OperationCanceledException)
        {
        }
        await fixture.Runtime.HaltQuiescence.WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
    }

    private static async Task RequireTerminal(DistributionLifecycleHostFixture fixture, DistributionLifecycleHostConnection connection)
    {
        var packet = await fixture.Take(connection);
        Check(packet.Payload is null && packet.Control.Equals(Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit),
            fixture.Producer.Process.Pid,
            connection.Remote,
            Term.A(ProcessExitReasons.Normal)
        )));
    }

    private static int MonitorCount(ProcessRuntime runtime)
    {
        lock (RuntimeGate(runtime))
        {
            var collection = (ICollection)typeof(ProcessRuntime).GetField(
                DistributionPublicationLifecycleHostNames.MonitorMember,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(runtime)!;

            return collection.Count;
        }
    }

    private static bool HasLink(ProcessRuntime runtime, ProcessContext process, Pid remote)
    {
        lock (RuntimeGate(runtime))
        {
            var links = (IEnumerable)typeof(ProcessContext).GetProperty(
                DistributionPublicationLifecycleHostNames.LinksMember,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(process)!;

            return links.Cast<Pid>().Contains(remote);
        }
    }

    private static object RuntimeGate(ProcessRuntime runtime) => typeof(ProcessRuntime).GetField(
        DistributionPublicationLifecycleHostNames.GateMember,
        BindingFlags.Instance | BindingFlags.NonPublic
    )!.GetValue(runtime)!;

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Check(bool condition) => DistributionLifecycleHostFixture.Check(condition);
}
