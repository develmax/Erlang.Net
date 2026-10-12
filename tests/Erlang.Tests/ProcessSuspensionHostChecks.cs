namespace Erlang.Tests;

internal static class ProcessSuspensionHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = runtime.Spawn(async context =>
        {
            started.TrySetResult();
            await context.ReceiveAsync(message => message as Atom, TimeSpan.FromMilliseconds(ProcessSuspensionHostParameters.DeadlineMilliseconds));
            received.TrySetResult();

            return Term.A(ErlangBooleanAtoms.True);
        });
        var owner = runtime.Spawn(async context =>
        {
            await started.Task;
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionNames.Suspend,
                [target.Pid]
            );
            var reference = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionHostNames.SendAfter,
                [Term.I(ProcessSuspensionHostParameters.DeadlineMilliseconds), target.Pid, Term.A(ProcessSuspensionHostNames.Ping)]
            );
            Term firstRemaining = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionHostNames.ReadTimer,
                [reference]
            );
            await Task.Delay(ProcessSuspensionHostParameters.PausedWaitMilliseconds);
            Require(!received.Task.IsCompleted);
            Term secondRemaining = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionHostNames.ReadTimer,
                [reference]
            );
            Require(firstRemaining.Equals(secondRemaining) && firstRemaining.Equals(Term.A(ErlangBooleanAtoms.False)));
            Term canceled = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionHostNames.CancelTimer,
                [reference]
            );
            Require(canceled.Equals(Term.A(ErlangBooleanAtoms.False)));
            Term cpc = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessCodeCheckNames.Function,
                [target.Pid, Term.A(ErlangModuleNames.Module)]
            );
            Require(cpc.Equals(Term.A(ErlangBooleanAtoms.False)));
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionNames.Resume,
                [target.Pid]
            );
            Term afterResume = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionHostNames.ReadTimer,
                [reference]
            );
            Require(afterResume.Equals(Term.A(ErlangBooleanAtoms.False)));
            await received.Task.WaitAsync(ProcessSuspensionHostParameters.CompletionTimeout);

            return Term.A(ErlangBooleanAtoms.True);
        });
        Require((await owner.Completion.WaitAsync(ProcessSuspensionHostParameters.CompletionTimeout)).Equals(Term.A(ProcessExitReasons.Normal)));
        await target.Completion.WaitAsync(ProcessSuspensionHostParameters.CompletionTimeout);
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var counted = runtime.Spawn(async context =>
        {
            await context.ReceiveAsync(message => message as Atom);

            return Term.A(ErlangBooleanAtoms.True);
        });
        var secondOwner = runtime.Spawn(async context =>
        {
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ProcessSuspensionNames.Suspend,
                [counted.Pid]
            );
            paused.TrySetResult();
            await context.ReceiveAsync(message => message as Atom);

            return Term.A(ErlangBooleanAtoms.True);
        });
        await paused.Task.WaitAsync(ProcessSuspensionHostParameters.CompletionTimeout);
        runtime.Send(counted.Pid, Term.A(ProcessSuspensionHostNames.Ping));
        Require(!counted.Completion.IsCompleted);
        runtime.Send(secondOwner.Pid, Term.A(ProcessSuspensionHostNames.Ping));
        Require((await secondOwner.Completion.WaitAsync(ProcessSuspensionHostParameters.CompletionTimeout)).Equals(Term.A(ProcessExitReasons.Normal)));
        Require((await counted.Completion.WaitAsync(ProcessSuspensionHostParameters.CompletionTimeout)).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessSuspensionHostNames.Failure);
    }
}
