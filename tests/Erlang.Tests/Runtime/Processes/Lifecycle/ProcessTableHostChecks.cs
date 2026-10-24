namespace Erlang.Tests;

public static class ProcessTableHostChecks
{
    public static async Task Run()
    {
        await CapacityAndReuse();
        await BatchedWalk();
    }

    private static async Task CapacityAndReuse()
    {
        await using var runtime = new ProcessRuntime(maximumProcesses: ProcessTableHostParameters.SmallCapacity);
        var parent = runtime.Spawn(async context =>
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var child = runtime.Spawn(async _ =>
            {
                await release.Task;

                return Nil.Value;
            }, context);
            int excessBodyRuns = 0;
            bool rejected = false;
            try
            {
                runtime.Spawn(_ =>
                {
                    Interlocked.Increment(ref excessBodyRuns);

                    return ValueTask.FromResult<Term>(Nil.Value);
                }, context);
            }
            catch (ErlangException exception) when (exception.Reason is Atom { Name: ProcessTableHostNames.SystemLimit })
            {
                rejected = true;
            }
            finally
            {
                release.TrySetResult();
            }
            if (!rejected || excessBodyRuns != 0 || runtime.ProcessLimit != ProcessTableHostParameters.SmallCapacity)
                throw new InvalidOperationException(ProcessTableHostDiagnostics.Capacity);
            await child.Completion.WaitAsync(ProcessTableHostParameters.Deadline);
            var replacementRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var observed = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
            var replacement = runtime.Spawn(async active =>
            {
                observed.SetResult(active);
                await replacementRelease.Task;

                return Nil.Value;
            }, context);
            try
            {
                var active = await observed.Task.WaitAsync(ProcessTableHostParameters.Deadline);
                if (child.Pid.Equals(replacement.Pid) || runtime.IsAlive(child.Pid) || !runtime.IsAlive(replacement.Pid))
                    throw new InvalidOperationException(ProcessTableHostDiagnostics.Reuse);
                runtime.Send(child.Pid, Term.I(1));
                runtime.Send(replacement.Pid, Term.I(2));
                if (active.Mailbox.Count != 1 || !active.Mailbox.RemoveFirst(message => message.Equals(Term.I(2))) || active.Mailbox.Count != 0)
                    throw new InvalidOperationException(ProcessTableHostDiagnostics.Stale);
                var ids = await Collect(context);
                if (ids.Count != ProcessTableHostParameters.SmallCapacity || !ids.Contains(replacement.Pid) || ids.Contains(child.Pid))
                    throw new InvalidOperationException(ProcessTableHostDiagnostics.Walk);
            }
            finally
            {
                replacementRelease.TrySetResult();
            }
            await replacement.Completion.WaitAsync(ProcessTableHostParameters.Deadline);

            return Nil.Value;
        });
        if (await parent.Completion.WaitAsync(ProcessTableHostParameters.Deadline) is not Atom { Name: ProcessTableHostNames.Normal })
            throw new InvalidOperationException(ProcessTableHostDiagnostics.Termination);
        if (runtime.ProcessSnapshot().Count != 1)
            throw new InvalidOperationException(ProcessTableHostDiagnostics.Termination);
    }

    private static async Task BatchedWalk()
    {
        await using var runtime = new ProcessRuntime(maximumProcesses: ProcessTableHostParameters.BatchCapacity);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var children = new List<ProcessHandle>();
        var parent = runtime.Spawn(async context =>
        {
            try
            {
                for (int index = 0; index < ProcessTableHostParameters.HeldChildren; index++)
                    children.Add(runtime.Spawn(async _ =>
                    {
                        await release.Task;

                        return Nil.Value;
                    }, context));
                var expected = runtime.ProcessSnapshot();
                var actual = await Collect(context);
                if (actual.Count != expected.Count || !actual.SetEquals(expected))
                    throw new InvalidOperationException(ProcessTableHostDiagnostics.Walk);
            }
            finally
            {
                release.TrySetResult();
            }

            return Nil.Value;
        });
        var reason = await parent.Completion.WaitAsync(ProcessTableHostParameters.Deadline);
        await Task.WhenAll(children.Select(child => child.Completion)).WaitAsync(ProcessTableHostParameters.Deadline);
        if (reason is not Atom { Name: ProcessTableHostNames.Normal } || runtime.ProcessSnapshot().Count != 1)
            throw new InvalidOperationException(ProcessTableHostDiagnostics.Termination);
    }

    private static async Task<HashSet<Pid>> Collect(ProcessContext context)
    {
        Term iterator = await context.Runtime.Modules.Call(context, ProcessTableHostNames.Module, ProcessTableHostNames.Iterator);
        var result = new HashSet<Pid>();
        while (true)
        {
            Term next = await context.Runtime.Modules.Call(
                context,
                ProcessTableHostNames.Module,
                ProcessTableHostNames.Next,
                iterator
            );
            if (next is Atom { Name: ProcessTableHostNames.None })
                break;
            if (next is not TupleTerm { Items.Count: 2 } pair || pair.Items[0] is not Pid pid || !result.Add(pid))
                throw new InvalidOperationException(ProcessTableHostDiagnostics.Walk);
            iterator = pair.Items[1];
        }

        return result;
    }
}
