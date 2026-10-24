using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class DistributionControlWaitPublicRouteHostChecks
{
    internal static async Task RunQueries()
    {
        await QueryDuringCapacity();
        await QueryDuringTurn();
    }

    internal static async Task RunKills()
    {
        await KillDuringCapacity();
        await KillDuringTurn();
    }

    internal static Task QueryDuringCapacity() => Query(false, DistributionControlWaitHostNames.Link);

    internal static Task QueryDuringTurn() => Query(true, DistributionControlWaitHostNames.Link);

    internal static Task KillDuringCapacity() => Kill(false, DistributionControlWaitHostNames.Link);

    internal static Task KillDuringTurn() => Kill(true, DistributionControlWaitHostNames.Link);

    internal static async Task Query(bool turn, string function)
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionControlWaitHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        int capacity = fixture.Fill(fixture.Connection);
        Check(capacity > 0);
        Task<Term>? blocker = turn ? await BlockFirstTicket(fixture) : null;
        var guest = Start(fixture, function, Arguments(fixture, function));
        Task<Term>? query = null;
        Exception? primary = null;
        try
        {
            guest.Start.TrySetResult();
            await guest.Entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Check(!guest.Guest.Completion.IsCompleted);
            RuntimeNodeFoundationHostActor requester = turn ? fixture.Connection.Controller : fixture.Producer;
            query = Information(
                fixture,
                requester,
                guest.Guest.Pid,
                ProcessInfoNames.Links
            );
            var reply = (TupleTerm)await query.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Term expected = function == DistributionControlWaitHostNames.Link ? Term.List(fixture.Connection.Remote) : Nil.Value;
            Check(reply.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), expected)));
            Check(!guest.Unwound.Task.IsCompleted);
            Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == capacity);
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        await Finish(
            fixture,
            guest.Guest,
            guest.Unwound.Task,
            blocker,
            query,
            primary
        );
    }

    internal static async Task Kill(bool turn, string function)
    {
        await using var fixture = await DistributionLifecycleHostFixture.Create();
        using var compiled = GeneratedModuleCompiler.Compile(DistributionControlWaitHostNames.Source);
        compiled.Definition.Register(fixture.Runtime.Modules);
        int capacity = fixture.Fill(fixture.Connection);
        Check(capacity > 0);
        Task<Term>? blocker = turn ? await BlockFirstTicket(fixture) : null;
        var guest = Start(fixture, function, Arguments(fixture, function));
        Exception? primary = null;
        try
        {
            RuntimeNodeFoundationHostActor observer = fixture.Connection.Controller;
            Term reference = await Monitor(fixture, observer, guest.Guest.Pid);
            guest.Start.TrySetResult();
            await guest.Entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Check(!guest.Unwound.Task.IsCompleted);
            if (function == DistributionControlWaitHostNames.Link)
            {
                var links = await Information(
                    fixture,
                    observer,
                    guest.Guest.Pid,
                    ProcessInfoNames.Links
                )
                    .WaitAsync(DistributionControlWaitHostNames.Deadline);
                Check(links.Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Term.List(fixture.Connection.Remote))));
            }
            await PutExit(fixture, guest.Guest.Pid, Term.A(ProcessExitSignals.Kill));
            Term? down = await Down(observer, reference);
            Check(down?.Equals(Term.Tuple(
                Term.A(ProcessMessageTags.MonitorDown),
                reference,
                Term.A(ProcessMonitorKinds.Process),
                guest.Guest.Pid,
                Term.A(ProcessExitReasons.Killed)
            )) == true);
            // Actual generated guest finally must run before any output capacity is released.
            await guest.Unwound.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == capacity);
            await fixture.DrainFiller(fixture.Connection, capacity);
            if (blocker is not null)
            {
                Check((await blocker.WaitAsync(DistributionControlWaitHostNames.Deadline)).Equals(Term.A(DistributionControlWaitHostNames.Marker)));
                Check((await fixture.Take(fixture.Connection)).Payload?.Equals(Term.A(DistributionControlWaitHostNames.Marker)) == true);
            }
            if (function == DistributionControlWaitHostNames.Link)
            {
                Check((await fixture.Take(fixture.Connection)).Control.Equals(Term.Tuple(
                    Term.I(DistributionControlOpcodes.Link), guest.Guest.Pid, fixture.Connection.Remote)));
                var terminal = (await fixture.Take(fixture.Connection)).Control;
                Check(terminal.Equals(Term.Tuple(
                    Term.I(DistributionControlOpcodes.Exit),
                    guest.Guest.Pid,
                    fixture.Connection.Remote,
                    Term.A(ProcessExitReasons.Killed)
                )));
            }
            await guest.Guest.Completion.WaitAsync(DistributionControlWaitHostNames.Deadline);
            Check(fixture.Runtime.DistributionPending.ConnectedSnapshot(fixture.Connection.Host).QueuedPackets == 0);
        }
        catch (Exception failure)
        {
            primary = failure;
        }
        await Finish(
            fixture,
            guest.Guest,
            guest.Unwound.Task,
            blocker,
            null,
            primary
        );
    }

    internal static (ProcessHandle Guest, TaskCompletionSource Start, TaskCompletionSource Entered,
        TaskCompletionSource Unwound, TaskCompletionSource<Term> Result) Start(
        DistributionLifecycleHostFixture fixture,
        string function,
        params Term[] arguments
    )
    {
        var start = Signal();
        var entered = Signal();
        var unwound = Signal();
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var guest = fixture.Runtime.Spawn(async context =>
        {
            try
            {
                await start.Task.WaitAsync(context.Cancellation);
                // Invoke genuine generated code. Its public BIF runs before its asynchronous operation returns here.
                ValueTask<Term> operation = fixture.Runtime.Modules.Call(
                    context,
                    DistributionControlWaitHostNames.GeneratedModule,
                    function,
                    arguments
                );
                entered.TrySetResult();
                Term value = await operation;
                result.TrySetResult(value);

                return value;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });

        return (guest, start, entered, unwound, result);
    }

    internal static Task<Term> Information(
        DistributionLifecycleHostFixture fixture,
        RuntimeNodeFoundationHostActor requester,
        Pid target,
        string key
    )
        => requester.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            target,
            Term.A(key)
        ));

    internal static Task<Term> Monitor(DistributionLifecycleHostFixture fixture,
        RuntimeNodeFoundationHostActor observer, Pid target)
        => observer.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionControlWaitHostNames.Monitor,
            Term.A(ProcessMonitorKinds.Process),
            target
        ));

    internal static Task<Term?> Down(RuntimeNodeFoundationHostActor observer, Term reference)
        => observer.InvokeAsync(async context => await context.ReceiveAsync<Term>(
            message =>
            message is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(Term.A(ProcessMessageTags.MonitorDown)) &&
            tuple.Items[1].Equals(reference) ? message : null,
            DistributionControlWaitHostNames.Deadline
        ));

    internal static async Task PutExit(DistributionLifecycleHostFixture fixture, Pid target, Term reason)
    {
        await Put(fixture, Term.Tuple(
            Term.I(DistributionControlOpcodes.Exit2),
            fixture.Connection.Remote,
            target,
            reason
        ));
    }

    internal static async Task Put(DistributionLifecycleHostFixture fixture, TupleTerm control)
    {
        byte[] packet = DistributionPassThroughCodec.EncodeProcessControl(control);
        Term result = await fixture.Connection.Controller.InvokeAsync(async context => await fixture.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            DistributionInputNames.Function,
            fixture.Connection.Opaque,
            new BitString(packet)
        ));
        Check(result.Equals(Term.A(ConnectedDistributionHostNames.Success)));
    }

    internal static async Task<Task<Term>> BlockFirstTicket(DistributionLifecycleHostFixture fixture)
    {
        var entered = Signal();
        Task<Term> blocker = fixture.Producer.InvokeAsync(async context =>
        {
            ValueTask<Term> operation = fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                DistributionControlWaitHostNames.SendFunction,
                fixture.Connection.Remote,
                Term.A(DistributionControlWaitHostNames.Marker)
            );
            entered.TrySetResult();

            return await operation;
        });
        await entered.Task.WaitAsync(DistributionControlWaitHostNames.Deadline);
        Check(!blocker.IsCompleted);

        return blocker;
    }

    internal static async Task Finish(
        DistributionLifecycleHostFixture fixture,
        ProcessHandle guest,
        Task unwound,
        Task<Term>? blocker,
        Task<Term>? query,
        Exception? primary
    )
    {
        var failures = new List<Exception>();
        if (primary is not null)
            failures.Add(primary);
        try
        {
            fixture.Runtime.DistributionPending.CloseConnected(fixture.Connection.Host);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        foreach (Task task in new Task?[] { unwound, guest.Completion, blocker, query }.OfType<Task>())
        {
            try
            {
                await task.WaitAsync(DistributionControlWaitHostNames.Deadline);
            }
            catch (Exception failure)
            {
                if (!ReferenceEquals(failure, primary))
                    failures.Add(failure);
            }
        }
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }

    internal static Term[] Arguments(DistributionLifecycleHostFixture fixture, string function)
        => function is DistributionControlWaitHostNames.Send or DistributionControlWaitHostNames.Exit or DistributionControlWaitHostNames.Receive
            ? [fixture.Connection.Remote, Term.A(DistributionControlWaitHostNames.Marker)] : [fixture.Connection.Remote];

    internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionControlWaitHostNames.Failure);
    }
}
