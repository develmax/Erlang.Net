namespace Erlang.Tests;

internal static class QueueCooperativeHostChecks
{
    internal static async Task Run()
    {
        Term input = Nil.Value;
        for (int index = 0; index < QueueCooperativeHostNames.Cells; index++)
            input = new Cons(Nil.Value, input);
        var failures = new List<Exception>();
        try
        {
            await QueueCoreReverseEntryHostChecks.Run();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        foreach (string function in new[]
        {
            QueueModuleNames.Length,
            QueueModuleNames.FromList,
            QueueModuleNames.ToList,
            QueueModuleNames.Out,
            QueueModuleNames.OutReverse,
            QueueModuleNames.Drop,
            QueueModuleNames.DropReverse,
            QueueModuleNames.Get,
            QueueModuleNames.GetReverse,
            QueueModuleNames.Peek,
            QueueModuleNames.Member
        })
        {
            foreach (bool cancel in new[] { false, true })
            {
                try
                {
                    await Boundary(function, input, cancel);
                }
                catch (Exception failure)
                {
                    failures.Add(new InvalidOperationException(function, failure));
                }
            }
        }
        foreach (bool cancel in new[] { false, true })
        {
            try
            {
                await Boundary(QueueModuleNames.Member, input, cancel, frontMember: true);
            }
            catch (Exception failure)
            {
                failures.Add(new InvalidOperationException(QueueModuleNames.Member, failure));
            }
        }
        Require(Cons.Items(input).Count() == QueueCooperativeHostNames.Cells);
        if (failures.Count != 0)
            throw new AggregateException(QueueCooperativeHostNames.Failure, failures);
    }

    private static async Task Boundary(string function, Term input, bool cancel, bool frontMember = false)
    {
        await using var runtime = new ProcessRuntime();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var cleanup = new RuntimeNodeFoundationHostActor(runtime);
        var continuations = new ListLengthYieldContext();
        var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Term[] arguments = function switch
        {
            QueueModuleNames.FromList => [input],
            QueueModuleNames.Out => [Term.Tuple(input, Term.List(Nil.Value))],
            QueueModuleNames.OutReverse => [Term.Tuple(Term.List(Nil.Value), input)],
            QueueModuleNames.DropReverse or QueueModuleNames.GetReverse => [Term.Tuple(Nil.Value, input)],
            QueueModuleNames.Member => [Term.I(QueueCooperativeHostNames.MissingMember),
                frontMember ? Term.Tuple(Nil.Value, input) : Term.Tuple(input, Nil.Value)],
            _ => [Term.Tuple(input, Nil.Value)]
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
                        QueueModuleNames.Module,
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
            pending = await entered.Task.WaitAsync(QueueCooperativeHostNames.Deadline);
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
                    && reductions.Value >= QueueCooperativeHostNames.MinimumObservedTraversalReductions);
                if (cancel)
                    runtime.Exit(context, worker.Pid, Term.A(QueueCooperativeHostNames.Kill));

                return true;
            });
            Require(progressed && !pending.IsCompleted);
            await continuations.ResumeUntil(pending);
            Term reason = await worker.Completion.WaitAsync(QueueCooperativeHostNames.Deadline);
            await unwound.Task.WaitAsync(QueueCooperativeHostNames.Deadline);
            Require(!runtime.IsAlive(worker.Pid));
            if (cancel)
            {
                Require(pending.IsCanceled && reason.Equals(Term.A(QueueCooperativeHostNames.Killed)));
            }
            else
            {
                Require(reason.Equals(Term.A(QueueCooperativeHostNames.Normal)));
                CheckResult(function, await pending);
            }
            Term aliveResult = await observer.InvokeAsync(async context =>
                await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.New));
            Require(aliveResult.Equals(Term.Tuple(Nil.Value, Nil.Value)));
        }
        finally
        {
            if (pending is null || !pending.IsCompleted)
            {
                await cleanup.Invoke(context =>
                {
                    runtime.Exit(context, worker.Pid, Term.A(QueueCooperativeHostNames.Kill));

                    return true;
                });
                if (pending is not null)
                    await continuations.ResumeUntil(pending);
            }
            await worker.Completion.WaitAsync(QueueCooperativeHostNames.Deadline);
            await unwound.Task.WaitAsync(QueueCooperativeHostNames.Deadline);
        }
    }

    private static void CheckResult(string function, Term value)
    {
        if (function is QueueModuleNames.Get or QueueModuleNames.GetReverse)
        {
            Require(value is Nil);
            return;
        }
        if (function == QueueModuleNames.Peek)
        {
            Require(value.Equals(Term.Tuple(Term.A(QueueCoreHostNames.Value), Nil.Value)));
            return;
        }
        if (function == QueueModuleNames.Member)
        {
            Require(value.Equals(Term.A(QueueCoreHostNames.False)));
            return;
        }
        if (function == QueueModuleNames.Length)
        {
            Require(value.Equals(Term.I(QueueCooperativeHostNames.Cells)));

            return;
        }
        if (function == QueueModuleNames.ToList)
        {
            Require(Cons.Items(value).Count() == QueueCooperativeHostNames.Cells
                && Cons.Items(value).All(item => item is Nil));

            return;
        }
        if (function is QueueModuleNames.Out or QueueModuleNames.OutReverse)
        {
            Require(value is TupleTerm { Items.Count: 2 });
            var removed = (TupleTerm)value;
            Require(removed.Items[0].Equals(Term.Tuple(Term.A(QueueCoreHostNames.Value), Nil.Value)));
            value = removed.Items[1];
        }
        Require(value is TupleTerm { Items.Count: 2 });
        var queue = (TupleTerm)value;
        Require(queue.Items[0] is Cons && queue.Items[1] is Cons);
        Term[] retained = Cons.Items(queue.Items[0]).Concat(Cons.Items(queue.Items[1])).ToArray();
        int expectedCount = function is QueueModuleNames.Drop or QueueModuleNames.DropReverse
            ? QueueCooperativeHostNames.Cells - 1 : QueueCooperativeHostNames.Cells;
        Require(retained.Length == expectedCount && retained.All(item => item is Nil));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueCooperativeHostNames.Failure);
    }
}
