using Erlang;
namespace Erlang.Tests;

internal static class InternalProcess425HostChecks
{
    internal static async Task StateAndReplies()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new InternalProcess425HostTarget(runtime);
        ProcessContext targetContext = await target.Ready;
        await actor.InvokeAsync(async context =>
        {
            var group2 = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Leader, 2);
            var group3 = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Leader, 3);
            InternalProcess425HostAccess.Require((await group2(context, new Term[] { context.Self, context.Self })).Equals(Term.A(InternalProcess425HostNames.True)));
            ReferenceTerm id = runtime.MakeReference();
            for (int i = 0; i < 2; i++)
                InternalProcess425HostAccess.Require((await group3(context, new Term[] { context.Self, context.Self, id })).Equals(Term.A(InternalProcess425HostNames.Ok)));
            for (int i = 0; i < 2; i++)
                InternalProcess425HostAccess.Require((await InternalProcess425HostAccess.Reply(context, id)).Equals(Term.A(InternalProcess425HostNames.True)));
            InternalProcess425HostAccess.Require(runtime.GetGroupLeader(context).Equals(context.Self));
            var flag = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Flag, 3);
            InternalProcess425HostAccess.Require((await InternalProcess425HostAccess.Resolve(context, flag(context, new Term[] { targetContext.Self, Term.A(InternalProcess425HostNames.SaveCalls), Term.I(4) }))).Equals(Term.I(0)));
            InternalProcess425HostAccess.Require((await InternalProcess425HostAccess.Resolve(context, flag(context, new Term[] { targetContext.Self, Term.A(InternalProcess425HostNames.SaveCalls), Term.I(0) }))).Equals(Term.I(4)));
            var display = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Display, 2);
            InternalProcess425HostAccess.Require((await display(context, new Term[] { Term.A(InternalProcess425HostNames.Bad), Term.A(InternalProcess425HostNames.Bad) })).Equals(Term.A(InternalProcess425HostNames.BadOption)));
            InternalProcess425HostAccess.Require((await InternalProcess425HostAccess.Resolve(context, display(context, new Term[] { targetContext.Self, Term.A(InternalProcess425HostNames.Backtrace) }))).Equals(Term.A(InternalProcess425HostNames.True)));

            return true;
        });
    }

    internal static async Task Suspension()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new InternalProcess425HostTarget(runtime);
        ProcessContext targetContext = await target.Ready;
        await actor.InvokeAsync(async context =>
        {
            var suspend = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Suspend, 2);
            InternalProcess425HostAccess.Require((await suspend(context, new Term[] { context.Self, Nil.Value })).Equals(Term.A(InternalProcess425HostNames.BadArgument)));
            ReferenceTerm tag = runtime.MakeReference();
            InternalProcess425HostAccess.Require((await suspend(context, new Term[] { targetContext.Self, Term.List(Term.Tuple(Term.A(InternalProcess425HostNames.Asynchronous), tag)) })).Equals(Term.A(InternalProcess425HostNames.True)));
            InternalProcess425HostAccess.Require((await InternalProcess425HostAccess.Reply(context, tag)).Equals(Term.A(InternalProcess425HostNames.Suspended)));
            InternalProcess425HostAccess.Require((await runtime.Modules.Call(
                context,
                InternalProcess425HostNames.Erlang,
                InternalProcess425HostNames.Resume,
                targetContext.Self
            )).Equals(Term.A(InternalProcess425HostNames.True)));

            return true;
        });
    }

    internal static async Task OwnersAndMonitor()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        var monitor = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Monitor, 1);
        await other.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await monitor(context, new Term[] { Term.A(InternalProcess425HostNames.Legacy) });
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            InternalProcess425HostAccess.Require(refused);

            return true;
        });
        await actor.InvokeAsync(async context =>
        {
            var configure = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Monitor, 3);
            InternalProcess425HostAccess.Require((await monitor(context, new Term[] { Term.A(InternalProcess425HostNames.Legacy) })).Equals(Term.A(InternalProcess425HostNames.Undefined)));
            InternalProcess425HostAccess.Require((await configure(context, new Term[] { Term.A(InternalProcess425HostNames.Legacy), Term.A(InternalProcess425HostNames.Undefined), Nil.Value })).Equals(Term.A(InternalProcess425HostNames.Undefined)));
            Term busy = Term.List(Term.A(InternalProcess425HostNames.BusyPort));
            Term setting = Term.Tuple(context.Self, busy);
            InternalProcess425HostAccess.Require((await configure(context, new Term[] { Term.A(InternalProcess425HostNames.Legacy), context.Self, busy })).Equals(Term.A(InternalProcess425HostNames.Undefined)));
            InternalProcess425HostAccess.Require((await monitor(context, new Term[] { Term.A(InternalProcess425HostNames.Legacy) })).Equals(setting));
            InternalProcess425HostAccess.Require((await configure(context, new Term[] { Term.A(InternalProcess425HostNames.Legacy), Term.A(InternalProcess425HostNames.Undefined), Nil.Value })).Equals(setting));
            bool bad = false;
            try
            {
                await monitor(context, new Term[] { runtime.MakeReference() });
            }
            catch (ErlangException error)
            {
                bad = error.Reason.Equals(Term.A(InternalProcess425HostNames.BadArgument));
            }
            InternalProcess425HostAccess.Require(bad);

            return true;
        });
    }

    internal static async Task TerminationReplies()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            InternalProcess425HostNames.Erlang,
            InternalProcess425HostNames.ProcessFlag,
            Term.A(InternalProcess425HostNames.Scheduler),
            Term.I(1)
        ));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle target = runtime.Spawn(async context =>
        {
            await runtime.Modules.Call(
                context,
                InternalProcess425HostNames.Erlang,
                InternalProcess425HostNames.ProcessFlag,
                Term.A(InternalProcess425HostNames.Scheduler),
                Term.I(2)
            );
            ready.TrySetResult(context);
            entered.Set();
            InternalProcess425HostAccess.Require(release.Wait(InternalProcess425HostNames.Deadline));
            context.Cancellation.ThrowIfCancellationRequested();

            return Nil.Value;
        });
        try
        {
            ProcessContext targetContext = await ready.Task.WaitAsync(InternalProcess425HostNames.Deadline);
            InternalProcess425HostAccess.Require(entered.Wait(InternalProcess425HostNames.Deadline));
            await actor.InvokeAsync(async context =>
            {
                var flag = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Flag, 3);
                var display = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Display, 2);
                var group = InternalProcess425HostAccess.Body(runtime, InternalProcess425HostNames.Leader, 3);
                Term flagId = await flag(context, new Term[] { targetContext.Self, Term.A(InternalProcess425HostNames.SaveCalls), Term.I(4) });
                Term displayId = await display(context, new Term[] { targetContext.Self, Term.A(InternalProcess425HostNames.Backtrace) });
                ReferenceTerm groupId = runtime.MakeReference();
                await group(context, new Term[] { context.Self, targetContext.Self, groupId });
                InternalProcess425HostAccess.Require(flagId is ReferenceTerm && displayId is ReferenceTerm);
                runtime.Exit(context, targetContext.Self, Term.A(InternalProcess425HostNames.Kill));
                release.Set();
                foreach (var id in new[] { (ReferenceTerm)flagId, (ReferenceTerm)displayId, groupId })
                    InternalProcess425HostAccess.Require((await InternalProcess425HostAccess.Reply(context, id)).Equals(Term.A(InternalProcess425HostNames.BadArgument)));

                return true;
            });
        }
        finally
        {
            release.Set();
            await target.Completion.WaitAsync(InternalProcess425HostNames.Deadline);
        }
    }

}
