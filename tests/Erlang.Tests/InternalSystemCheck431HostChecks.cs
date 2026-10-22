using Erlang;
namespace Erlang.Tests;

internal static class InternalSystemCheck431HostChecks
{
    internal static async Task StatusAndOwner()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var body = InternalSystemCheck431HostAccess.Body(runtime);
        await actor.InvokeAsync(async context =>
        {
            bool bad = false;
            try
            {
                await body(context, [Term.I(17)]);
            }
            catch (ErlangException error)
            {
                bad = error.Reason.Equals(Term.A(InternalSystemCheck431HostNames.BadArgument));
            }
            InternalSystemCheck431HostAccess.Require(bad);
            bool held = false;
            try
            {
                await body(context, [Term.A(InternalSystemCheck431HostNames.Schedulers)]);
            }
            catch (NotSupportedException)
            {
                held = true;
            }
            InternalSystemCheck431HostAccess.Require(held);

            return true;
        });
        await using var foreign = new ProcessRuntime(1);
        await foreign.InitializeModulesAsync(_ => { });
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        await other.InvokeAsync(async context =>
{
    bool refused = false;
    try
    {
        await body(context, [Term.I(17)]);
    }
    catch (InvalidOperationException)
    {
        refused = true;
    }
    InternalSystemCheck431HostAccess.Require(refused);

    return true;
});
    }

    internal static async Task ActualAcks()
    {
        await using var runtime = new ProcessRuntime(InternalSystemCheck431HostNames.Participants);
        await InternalSystemCheck431HostAccess.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        var body = InternalSystemCheck431HostAccess.Body(runtime);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Task blocker = InternalSystemCheck431HostAccess.BlockLane(runtime, entered, release);
        Task<Term>? running = null;
        try
        {
            await entered.Task.WaitAsync(InternalSystemCheck431HostNames.Deadline);
            running = actor.InvokeAsync(async caller =>
{
    caller.Mailbox.Send(Term.A(InternalSystemCheck431HostNames.Sentinel));

    return await body(caller, [Term.A(InternalSystemCheck431HostNames.Schedulers)]);
});
            await InternalSystemCheck431HostAccess.WaitForAck(context);
            InternalSystemCheck431HostAccess.Require(!running.IsCompleted);
        }
        finally
        {
            release.Set();
            await blocker.WaitAsync(InternalSystemCheck431HostNames.Deadline);
        }
        InternalSystemCheck431HostAccess.Require((await running!.WaitAsync(InternalSystemCheck431HostNames.Deadline)).Equals(Term.A(InternalSystemCheck431HostNames.Ok)));
        await actor.InvokeAsync(async caller =>
        {
            Term? preserved = await caller.Mailbox.ReceiveAsync<Term>(term => term, InternalSystemCheck431HostNames.Deadline, caller.Cancellation);
            InternalSystemCheck431HostAccess.Require(preserved!.Equals(Term.A(InternalSystemCheck431HostNames.Sentinel)) && caller.Mailbox.Count == 0);

            return true;
        });
    }

    internal static async Task CallerExitJoin()
    {
        await using var runtime = new ProcessRuntime(InternalSystemCheck431HostNames.Participants);
        await InternalSystemCheck431HostAccess.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        var body = InternalSystemCheck431HostAccess.Body(runtime);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Task blocker = InternalSystemCheck431HostAccess.BlockLane(runtime, entered, release);
        Task<Term>? running = null;
        try
        {
            await entered.Task.WaitAsync(InternalSystemCheck431HostNames.Deadline);
            running = actor.InvokeAsync(async caller => await body(caller, [Term.A(InternalSystemCheck431HostNames.Schedulers)]));
            await InternalSystemCheck431HostAccess.WaitForAck(context);
            await controller.Invoke(caller =>
{
    runtime.Exit(caller, actor.Process.Pid, Term.A(InternalSystemCheck431HostNames.Kill));

    return true;
});
            long started = Environment.TickCount64;
            while (!context.Cancellation.IsCancellationRequested && Environment.TickCount64 - started < InternalSystemCheck431HostNames.Deadline.TotalMilliseconds) await Task.Delay(1);
            InternalSystemCheck431HostAccess.Require(context.Cancellation.IsCancellationRequested && !running.IsCompleted);
        }
        finally
        {
            release.Set();
            await blocker.WaitAsync(InternalSystemCheck431HostNames.Deadline);
        }
        bool cancelled = false;
        try
        {
            await running!.WaitAsync(InternalSystemCheck431HostNames.Deadline);
        }
        catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
        {
            cancelled = true;
        }
        catch (ErlangException error) when (context.Cancellation.IsCancellationRequested && error.ExceptionClass == ErlangExceptionClasses.Exit && error.Reason.Equals(Term.A(InternalSystemCheck431HostNames.Killed)))
        {
            cancelled = true;
        }
        InternalSystemCheck431HostAccess.Require(cancelled);
        await actor.Process.Completion.WaitAsync(InternalSystemCheck431HostNames.Deadline);
        InternalSystemCheck431HostAccess.Require(!InternalSystemCheck431HostAccess.Snapshot(context).Any(value => value is ReferenceTerm));
    }
}
