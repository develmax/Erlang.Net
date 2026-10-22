namespace Erlang.Tests;

internal static class ProcessAlive425HostChecks
{
    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(ProcessAlive425HostNames.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext context, Term pid, Term reference)
        => context.Runtime.Modules.Call(
            context,
            ProcessAlive425HostNames.Module,
            ProcessAlive425HostNames.Function,
            pid,
            reference
        );

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapProcessAlive425.Register(runtime, registry);
            RuntimeBootstrapProcessAlive425.Register(runtime, registry);
        });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        bool rejected = false;
        try
        {
            RuntimeBootstrapProcessAlive425.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            ReferenceTerm reference = runtime.MakeReference();
            Require((await Call(context, context.Self, reference)).Equals(Term.A(ProcessAlive425HostNames.Ok)));
            Term? reply = await context.Mailbox.ReceiveAsync<Term>(
                value => value is TupleTerm pair && pair.Items[0].Equals(reference) ? value : null,
                ProcessAlive425HostNames.Deadline,
                context.Cancellation
            );
            Require(reply!.Equals(Term.Tuple(reference, Term.A(ProcessAlive425HostNames.True))));
            foreach (Term invalid in new Term[] { Term.I(0), Nil.Value, Term.Tuple(), foreign.MakeReference(), runtime.ReferenceResources.CreateBinary(new byte[] { 1 }) })
            {
                bool bad = false;
                try
                {
                    await Call(context, context.Self, invalid);
                }
                catch (ErlangException error)
                {
                    bad = error.Reason.Equals(Term.A(ProcessAlive425HostNames.BadArgument));
                }
                Require(bad);
            }

            return true;
        });
    }

    internal static async Task Ordering()
    {
        foreach (bool kill in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime(2);
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapProcessAlive425.Register(runtime, registry));
            await using var requester = new RuntimeNodeFoundationHostActor(runtime);
            await using var target = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext targetContext = await target.Ready;
            _ = await requester.Ready;
            await requester.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ProcessAlive425HostNames.Runtime,
                ProcessAlive425HostNames.ProcessFlag,
                Term.A(ProcessAlive425HostNames.Scheduler),
                Term.I(1)
            ));
            await target.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ProcessAlive425HostNames.Runtime,
                ProcessAlive425HostNames.ProcessFlag,
                Term.A(ProcessAlive425HostNames.Scheduler),
                Term.I(2)
            ));
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            Task<bool>? held = null;
            try
            {
                held = target.InvokeAsync(async context =>
                {
                    entered.Set();
                    Require(release.Wait(ProcessAlive425HostNames.Deadline));
                    try
                    {
                        await context.Mailbox.ReceiveAsync<Term>(_ => null, TimeSpan.Zero, context.Cancellation);
                    }
                    catch (OperationCanceledException) when (kill && context.Cancellation.IsCancellationRequested) { }

                    return true;
                });
                Require(entered.Wait(ProcessAlive425HostNames.Deadline));
                ReferenceTerm reference = runtime.MakeReference();
                Term? earlyReply = null;
                await requester.InvokeAsync(async context =>
                {
                    if (kill) runtime.Exit(context, targetContext.Self, Term.A(ProcessAlive425HostNames.Kill));
                    Require((await Call(context, targetContext.Self, reference)).Equals(Term.A(ProcessAlive425HostNames.Ok)));
                    Term? early = await context.Mailbox.ReceiveAsync<Term>(value => value is TupleTerm pair && pair.Items[0].Equals(reference) ? value : null,
                        TimeSpan.Zero, context.Cancellation);
                    Require(early is null || kill && !runtime.IsAlive(targetContext.Self)
                        && early.Equals(Term.Tuple(reference, Term.A(ProcessAlive425HostNames.False))));
                    earlyReply = early;

                    return true;
                });
                release.Set();
                await held;
                Term? result = earlyReply ?? await requester.InvokeAsync(async context => await context.Mailbox.ReceiveAsync<Term>(
                    value => value is TupleTerm pair && pair.Items[0].Equals(reference) ? value : null,
                    ProcessAlive425HostNames.Deadline,
                    context.Cancellation
                ));
                Require(result!.Equals(Term.Tuple(reference, Term.A(kill ? ProcessAlive425HostNames.False : ProcessAlive425HostNames.True))));
                if (kill) Require((await target.Process.Completion.WaitAsync(ProcessAlive425HostNames.Deadline)).Equals(Term.A(ProcessAlive425HostNames.Killed)));
            }
            finally
            {
                release.Set();
                if (held is not null) await held;
            }
        }
    }
}
