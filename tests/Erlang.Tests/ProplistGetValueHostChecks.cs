using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

public static class ProplistGetValueHostChecks
{
    public static Task ParseAll()
    {
        var failures = new List<Exception>();
        foreach (var row in ProplistGetValueCases.Modules)
        {
            try
            {
                new Parser(row.Source).ParseModule();
            }
            catch (CompileException failure)
            {
                failures.Add(new InvalidOperationException(row.Name, failure));
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(ProplistGetValueHostNames.Failure, failures);

        return Task.CompletedTask;
    }

    public static async Task Identity()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term key = Term.A(ProplistGetValueHostNames.Key);
        Term payload = Term.Tuple(Term.I(7), new BitString([1, 2, 3]));
        Term found = await owner.InvokeAsync(context => runtime.Modules.Call(
            context,
            ProplistGetValueHostNames.Module,
            ProplistGetValueHostNames.Function,
            key,
            Term.List(Term.Tuple(key, payload))
        ).AsTask()).WaitAsync(ProplistGetValueHostNames.Deadline);
        Term missing = await owner.InvokeAsync(context => runtime.Modules.Call(
            context,
            ProplistGetValueHostNames.Module,
            ProplistGetValueHostNames.Function,
            key,
            Nil.Value,
            payload
        ).AsTask()).WaitAsync(ProplistGetValueHostNames.Deadline);
        if (!ReferenceEquals(payload, found) || !ReferenceEquals(payload, missing))
            throw new InvalidOperationException(ProplistGetValueHostNames.Failure);
    }

    public static Task Progress() => Traversal(false);

    public static Task Cancellation() => Traversal(true);

    private static async Task Traversal(bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        // Reuse the existing deterministic continuation queue; it observes
        // production reductions without changing the lookup implementation.
        var queue = new ListLengthYieldContext();
        var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Term properties = Nil.Value;
        for (int index = 0; index < ProplistGetValueHostNames.Cells; index++)
            properties = new Cons(Term.A(ProplistGetValueHostNames.Other), properties);

        var process = runtime.Spawn(async context =>
        {
            Task<Term> operation;
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(queue);
            try
            {
                operation = runtime.Modules.Call(
                    context,
                    ProplistGetValueHostNames.Module,
                    ProplistGetValueHostNames.Function,
                    Term.A(ProplistGetValueHostNames.Key),
                    properties,
                    Nil.Value
                ).AsTask();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

            entered.TrySetResult(operation);
            try
            {
                return await operation;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        Task<Term> pending = await entered.Task.WaitAsync(ProplistGetValueHostNames.Deadline);
        try
        {
            if (pending.IsCompleted)
                throw new InvalidOperationException(ProplistGetValueHostNames.Failure);

            bool progressed = await observer.Invoke(context =>
            {
                if (cancel)
                    runtime.Exit(context, process.Pid, Term.A(ProplistGetValueHostNames.Kill));

                return true;
            }).WaitAsync(ProplistGetValueHostNames.Deadline);
            if (!progressed || pending.IsCompleted)
                throw new InvalidOperationException(ProplistGetValueHostNames.Failure);

            await queue.ResumeUntil(pending);
            Term result = await process.Completion.WaitAsync(ProplistGetValueHostNames.Deadline);
            if (cancel)
            {
                if (!pending.IsCanceled || !result.Equals(Term.A(ProplistGetValueHostNames.Killed)))
                    throw new InvalidOperationException(ProplistGetValueHostNames.Failure);
            }
            else if (!(await pending).Equals(Nil.Value))
                throw new InvalidOperationException(ProplistGetValueHostNames.Failure);
        }
        finally
        {
            if (!pending.IsCompleted)
            {
                await observer.Invoke(context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ProplistGetValueHostNames.Kill));

                    return true;
                }).WaitAsync(ProplistGetValueHostNames.Deadline);
                await queue.ResumeUntil(pending);
            }

            await process.Completion.WaitAsync(ProplistGetValueHostNames.Deadline);
            await unwound.Task.WaitAsync(ProplistGetValueHostNames.Deadline);
        }
    }
}
