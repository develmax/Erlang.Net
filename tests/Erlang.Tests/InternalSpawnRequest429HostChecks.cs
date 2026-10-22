using Erlang;
namespace Erlang.Tests;

internal static class InternalSpawnRequest429HostChecks
{
    internal static async Task StatusAndOwner()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var body = InternalSpawnRequest429HostAccess.Body(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (Term[] args in new[]{new Term[]{Term.I(17),Term.A(InternalSpawnRequest429HostNames.Target),Nil.Value,Nil.Value},
                new Term[]{Term.A(InternalSpawnRequest429HostNames.TargetModule),Term.A(InternalSpawnRequest429HostNames.Target),new Cons(Term.I(1),Term.I(2)),Nil.Value}})
                InternalSpawnRequest429HostAccess.Require((await body(context, args)).Equals(Term.A(InternalSpawnRequest429HostNames.BadArgument)));
            Term[] invalid = InternalSpawnRequest429HostAccess.Arguments(new Cons(Term.A(InternalSpawnRequest429HostNames.Monitor), Term.I(2)));
            InternalSpawnRequest429HostAccess.Require((await body(context, invalid)).Equals(Term.A(InternalSpawnRequest429HostNames.BadOptions)));
            ReferenceTerm reference = (ReferenceTerm)await body(context, InternalSpawnRequest429HostAccess.Arguments(Term.List(Term.I(17))));
            InternalSpawnRequest429HostAccess.Require((await InternalSpawnRequest429HostAccess.ErrorReply(context, reference)).Equals(Term.Tuple(
                Term.A(InternalSpawnRequest429HostNames.Reply),
                reference,
                Term.A(InternalSpawnRequest429HostNames.Error),
                Term.A(InternalSpawnRequest429HostNames.BadOptions)
            )));
            bool held = false;
            try
            {
                await body(
                    context,
                    InternalSpawnRequest429HostAccess.Arguments(Term.List(Term.Tuple(Term.A(InternalSpawnRequest429HostNames.MinimumHeap), Term.I(233))))
                );
            }
            catch (NotSupportedException)
            {
                held = true;
            }
            InternalSpawnRequest429HostAccess.Require(held);

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
                await body(context, InternalSpawnRequest429HostAccess.Arguments(Nil.Value));
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            InternalSpawnRequest429HostAccess.Require(refused);

            return true;
        });
    }

    internal static async Task Abandonment()
    {
        await using var runtime = new ProcessRuntime(1);
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        await runtime.InitializeModulesAsync(registry => registry.Register(
            InternalSpawnRequest429HostNames.TargetModule,
            InternalSpawnRequest429HostNames.Target,
            0,
            async (context, _) =>
        {
            context.TrapExits = true;
            ready.TrySetResult(context);
            await release.Task.WaitAsync(context.Cancellation);
            Term? exit = await context.Mailbox.ReceiveAsync<Term>(term => term, InternalSpawnRequest429HostNames.Deadline, context.Cancellation);
            observed.TrySetResult(exit!);

            return Nil.Value;
        }
        ));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var body = InternalSpawnRequest429HostAccess.Body(runtime);
        ProcessContext owner = await actor.Ready;
        ReferenceTerm reference = (ReferenceTerm)await actor.InvokeAsync(async context => await body(
            context,
            InternalSpawnRequest429HostAccess.Arguments(
            Term.List(Term.A(InternalSpawnRequest429HostNames.Link), Term.A(InternalSpawnRequest429HostNames.Monitor)))
        ));
        ProcessContext child = await ready.Task.WaitAsync(InternalSpawnRequest429HostNames.Deadline);
        await actor.InvokeAsync(async context =>
        {
            Term result = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSpawnRequestNames.AbandonFunction,
                reference
            );
            InternalSpawnRequest429HostAccess.Require(result.Equals(Term.A(ErlangBooleanAtoms.True)));
            InternalSpawnRequest429HostAccess.Require((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSpawnRequestNames.AbandonFunction,
                reference
            )).Equals(Term.A(ErlangBooleanAtoms.False)));

            return true;
        });
        release.TrySetResult();
        InternalSpawnRequest429HostAccess.Require((await observed.Task.WaitAsync(InternalSpawnRequest429HostNames.Deadline)).Equals(Term.Tuple(
            Term.A(InternalSpawnRequest429HostNames.Exit), owner.Self, Term.A(InternalSpawnRequest429HostNames.Abandoned))));
        await actor.Invoke(context =>
{
    InternalSpawnRequest429HostAccess.Require(context.Mailbox.Count == 0);

    return true;
});
    }

    internal static async Task OwnerDeath()
    {
        await using var runtime = new ProcessRuntime(1);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        await runtime.InitializeModulesAsync(registry => registry.Register(
            InternalSpawnRequest429HostNames.TargetModule,
            InternalSpawnRequest429HostNames.Target,
            0,
            async (context, _) =>
        {
            context.TrapExits = true;
            ready.TrySetResult();
            Term? exit = await context.Mailbox.ReceiveAsync<Term>(term => term, InternalSpawnRequest429HostNames.Deadline, context.Cancellation);
            observed.TrySetResult(exit!);

            return Nil.Value;
        }
        ));
        var body = InternalSpawnRequest429HostAccess.Body(runtime);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            await body(
                context,
                InternalSpawnRequest429HostAccess.Arguments(Term.List(Term.A(InternalSpawnRequest429HostNames.Link), Term.A(InternalSpawnRequest429HostNames.Monitor)))
            );
            await ready.Task.WaitAsync(context.Cancellation);
            context.Exit(Term.A(InternalSpawnRequest429HostNames.OwnerDeath));

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(InternalSpawnRequest429HostNames.Deadline);
        InternalSpawnRequest429HostAccess.Require((await observed.Task.WaitAsync(InternalSpawnRequest429HostNames.Deadline)).Equals(Term.Tuple(
            Term.A(InternalSpawnRequest429HostNames.Exit), owner.Pid, Term.A(InternalSpawnRequest429HostNames.OwnerDeath))));
    }
}
