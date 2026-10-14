namespace Erlang.Tests;

public static class PriorityPermissionHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var firstReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = runtime.Spawn(async context =>
        {
            firstReady.SetResult(context);
            await release.Task.WaitAsync(context.Cancellation);

            return Nil.Value;
        });
        var second = runtime.Spawn(async context =>
        {
            secondReady.SetResult(context);
            await release.Task.WaitAsync(context.Cancellation);

            return Nil.Value;
        });
        var source = await firstReady.Task.WaitAsync(PriorityPermissionHostNames.Timeout);
        var target = await secondReady.Task.WaitAsync(PriorityPermissionHostNames.Timeout);
        try
        {
            for (int round = 0; round < PriorityPermissionHostNames.Rounds; round++)
            {
                runtime.Link(target, source.Self, true);
                Require(source.Mailbox.Count == 0);
                await RequireFlag(source, target.Self, true);
                runtime.Unlink(source, target.Self);
                await RequireFlag(source, target.Self, true);
                runtime.Link(source, target.Self);
                Require(target.Mailbox.Count == 0);
                await RequireFlag(source, target.Self, false);
                Require(source.Mailbox.Count == 0);
                runtime.Link(target, source.Self, true);
                await RequireFlag(source, target.Self, true);
                runtime.Unlink(source, target.Self);
                await RequireFlag(source, target.Self, true);
                Require(target.Mailbox.Count == 0);
                await RequireFlag(source, target.Self, false);
                await RequireFlag(source, source.Self, false);
            }
            var aliases = Enumerable.Range(0, PriorityPermissionHostNames.Rounds).Select(_ => runtime.CreateAlias(target, true, true)).ToArray();
            await RequireFlag(source, target.Self, true);
            Parallel.For(0, aliases.Length, index => runtime.Send(aliases[index], Term.I(index), true));
            Require(target.Mailbox.Count == aliases.Length);
            await RequireFlag(source, target.Self, false);
            foreach (var alias in aliases)
                Require(!runtime.Unalias(target, alias));
            for (int index = 0; index < aliases.Length; index++)
                Require(await target.Mailbox.ReceiveAsync<Term>(message => message, TimeSpan.Zero) is Integer);
            Require(target.Mailbox.Count == 0);
            Require(source.Mailbox.Count == 0);
            for (int round = 0; round < PriorityPermissionHostNames.Rounds; round++)
            {
                runtime.Link(source, target.Self, true);
                runtime.Link(target, source.Self, true);
                runtime.Unlink(source, target.Self);
                runtime.Unlink(target, source.Self);
                await RequireFlag(source, source.Self, false);
                await RequireFlag(source, target.Self, false);
                Require(target.Mailbox.Count == 0);
                Require(source.Mailbox.Count == 0);
                Require(target.Mailbox.Count == 0);
                Require(LinkIdentityCount(source) == 0 && LinkIdentityCount(target) == 0);
            }
            await runtime.Modules.Call(
                source,
                ErlangModuleNames.Module,
                PriorityPermissionHostNames.ProcessFlag,
                Term.A(PriorityPermissionHostNames.TrapExit),
                Term.A(ErlangBooleanAtoms.True)
            );
            await runtime.Modules.Call(
                source,
                ErlangModuleNames.Module,
                ProcessTraceNames.Configure,
                source.Self,
                Term.A(ErlangBooleanAtoms.True),
                Term.List(Term.A(ProcessTraceNames.Receive), Term.Tuple(Term.A(ProcessTraceNames.Tracer), target.Self))
            );
            for (int round = 0; round < PriorityPermissionHostNames.PendingLinkDeathRounds; round++)
            {
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var victim = runtime.Spawn(async child =>
                {
                    ready.SetResult();
                    await release.Task.WaitAsync(child.Cancellation);

                    return Nil.Value;
                });
                await ready.Task.WaitAsync(PriorityPermissionHostNames.Timeout);
                runtime.Link(source, victim.Pid, true);
                await RequireFlag(source, source.Self, true);
                bool unlinked = round % 2 == 0;
                if (unlinked)
                    runtime.Unlink(source, victim.Pid);
                runtime.Exit(source, victim.Pid, Term.A(ProcessExitSignals.Kill));
                Require((await victim.Completion.WaitAsync(PriorityPermissionHostNames.Timeout)).Equals(Term.A(ProcessExitReasons.Killed)));
                if (unlinked)
                {
                    Require(source.Mailbox.Count == 0);
                    Require(target.Mailbox.Count == 0);
                }
                else
                {
                    var exit = Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), victim.Pid, Term.A(ErlangErrorReasons.NoProcess));
                    Require((await source.Mailbox.ReceiveAsync<Term>(message => message, TimeSpan.Zero))?.Equals(exit) == true);
                    var trace = Term.Tuple(
                        Term.A(ProcessTraceNames.Message),
                        source.Self,
                        Term.A(ProcessTraceNames.Receive),
                        exit
                    );
                    Require((await target.Mailbox.ReceiveAsync<Term>(message => message, TimeSpan.Zero))?.Equals(trace) == true);
                }
                await RequireFlag(source, source.Self, false);
                Require(LinkIdentityCount(source) == 0);
            }
            for (int round = 0; round < PriorityPermissionHostNames.DeadPeerRelinkRounds; round++)
            {
                var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
                var victim = runtime.Spawn(async child =>
                {
                    ready.SetResult(child);
                    await release.Task.WaitAsync(child.Cancellation);

                    return Nil.Value;
                });
                var victimContext = await ready.Task.WaitAsync(PriorityPermissionHostNames.Timeout);
                runtime.Link(source, victim.Pid, true);
                Require(victimContext.Mailbox.Count == 0);
                runtime.Unlink(source, victim.Pid);
                Require(victimContext.Mailbox.Count == 0);
                runtime.Exit(source, victim.Pid, Term.A(ProcessExitSignals.Kill));
                Require((await victim.Completion.WaitAsync(PriorityPermissionHostNames.Timeout)).Equals(Term.A(ProcessExitReasons.Killed)));
                runtime.Link(source, victim.Pid, true);
                RequireCallerLinkReactivated(source, victim.Pid);
                await RequireFlag(source, source.Self, true);
                var exit = Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), victim.Pid, Term.A(ErlangErrorReasons.NoProcess));
                Require((await source.Mailbox.ReceiveAsync<Term>(message => message, TimeSpan.Zero))?.Equals(exit) == true);
                var trace = Term.Tuple(
                    Term.A(ProcessTraceNames.Message),
                    source.Self,
                    Term.A(ProcessTraceNames.Receive),
                    exit
                );
                Require((await target.Mailbox.ReceiveAsync<Term>(message => message, TimeSpan.Zero))?.Equals(trace) == true);
                await RequireFlag(source, source.Self, false);
                Require(LinkIdentityCount(source) == 0);
            }
            await runtime.Modules.Call(
                source,
                ErlangModuleNames.Module,
                ProcessTraceNames.Configure,
                source.Self,
                Term.A(ErlangBooleanAtoms.False),
                Term.List(Term.A(ProcessTraceNames.Receive))
            );
        }
        finally
        {
            release.SetResult();
            await first.Completion.WaitAsync(PriorityPermissionHostNames.Timeout);
            await second.Completion.WaitAsync(PriorityPermissionHostNames.Timeout);
        }
    }

    private static void RequireCallerLinkReactivated(ProcessContext context, Pid target)
    {
        var property = typeof(ProcessContext).GetProperty(
            PriorityPermissionHostNames.LinkIdentityProperty,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        var identities = (System.Collections.IDictionary)property.GetValue(context)!;
        var identity = identities[target]!;
        var pending = identity.GetType().GetProperty(PriorityPermissionHostNames.PendingUnlinkProperty)!;
        Require(pending.GetValue(identity) is null);
    }

    private static int LinkIdentityCount(ProcessContext context)
    {
        var property = typeof(ProcessContext).GetProperty(
            PriorityPermissionHostNames.LinkIdentityProperty,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;

        return ((System.Collections.IDictionary)property.GetValue(context)!).Count;
    }

    private static async Task RequireFlag(ProcessContext context, Pid target, bool expected)
    {
        var value = context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            target,
            Term.A(ProcessInfoNames.PriorityMessages)
        );
        Require(value.IsCompletedSuccessfully);
        Require((await value).Equals(Term.Tuple(Term.A(ProcessInfoNames.PriorityMessages), Term.A(expected ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False))));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(PriorityPermissionHostNames.Failure);
    }
}
