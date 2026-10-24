namespace Erlang.Tests;

internal static class BinaryPrimitiveCooperativeHostChecks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        foreach (string function in new[] { BinaryModuleNames.ListToBin, BinaryModuleNames.Copy, BinaryModuleNames.LongestCommonPrefix, BinaryModuleNames.LongestCommonSuffix })
            foreach (bool cancel in new[] { false, true })
                try
                {
                    await Boundary(function, cancel);
                }
                catch (Exception error)
                {
                    failures.Add(new InvalidOperationException(function, error));
                }
        if (failures.Count != 0)
            throw new AggregateException(BinarySearchHostNames.Failure, failures);
    }

    private static async Task Boundary(string function, bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var cleanup = new RuntimeNodeFoundationHostActor(runtime);
        var continuations = new ListLengthYieldContext();
        var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Term[] arguments = function switch
        {
            BinaryModuleNames.ListToBin => [Cons.From(Enumerable.Repeat<Term>(Nil.Value, BinarySearchHostNames.SubjectBytes))],
            BinaryModuleNames.Copy => [new BitString([7]), Term.I(BinarySearchHostNames.SubjectBytes)],
            _ => [Term.List(new BitString(new byte[BinarySearchHostNames.SubjectBytes]), new BitString(new byte[BinarySearchHostNames.SubjectBytes]))]
        };
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                Task<Term> operation;
                SynchronizationContext? previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(continuations);
                try
                {
                    operation = runtime.Modules.Call(
                        context,
                        BinaryModuleNames.Module,
                        function,
                        arguments
                    ).AsTask();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previous);
                }
                entered.TrySetResult(operation);

                return await operation;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        Task<Term>? pending = null;
        try
        {
            pending = await entered.Task.WaitAsync(BinarySearchHostNames.Deadline);
            Require(!pending.IsCompleted && !unwound.Task.IsCompleted);
            bool progressed = await observer.InvokeAsync(async context =>
            {
                Require(!pending.IsCompleted);
                Term sampled = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    worker.Pid,
                    Term.A(ProcessInfoLogicalNames.Reductions)
                );
                Require(sampled is TupleTerm { Items.Count: 2 } sample
                    && sample.Items[1] is Integer reductions
                    && reductions.Value > 0);
                if (cancel)
                    runtime.Exit(context, worker.Pid, Term.A(BinarySearchHostNames.Kill));

                return true;
            });
            Require(progressed && !pending.IsCompleted);
            await continuations.ResumeUntil(pending);
            Term reason = await worker.Completion.WaitAsync(BinarySearchHostNames.Deadline);
            await unwound.Task.WaitAsync(BinarySearchHostNames.Deadline);
            Require(!runtime.IsAlive(worker.Pid));
            if (cancel)
            {
                Require(pending.IsCanceled && reason.Equals(Term.A(BinarySearchHostNames.Killed)));
            }
            else
            {
                Require(reason.Equals(Term.A(BinarySearchHostNames.Normal)));
                Term result = await pending;
                Require(function switch
                {
                    BinaryModuleNames.ListToBin => result is BitString { BitLength: 0 },
                    BinaryModuleNames.Copy => result is BitString copied && copied.BitLength == BinarySearchHostNames.SubjectBytes * 8 && copied.ToArray().All(value => value == 7),
                    _ => result.Equals(Term.I(BinarySearchHostNames.SubjectBytes))
                });
            }
            Term aliveResult = await observer.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Match,
                    new BitString([1]),
                    new BitString([2])
                ));
            Require(aliveResult.Equals(Term.A(BinarySearchHostNames.NoMatch)));
        }
        finally
        {
            if (pending is null || !pending.IsCompleted)
            {
                await cleanup.Invoke(context =>
                {
                    runtime.Exit(context, worker.Pid, Term.A(BinarySearchHostNames.Kill));

                    return true;
                });
                if (pending is not null)
                    await continuations.ResumeUntil(pending);
            }
            await worker.Completion.WaitAsync(BinarySearchHostNames.Deadline);
            await unwound.Task.WaitAsync(BinarySearchHostNames.Deadline);
        }
    }

    private static void Require(bool value) => BinarySearchMfaContracts.Require(value);
}
