using System.Reflection;

namespace Erlang.Tests;

public static class ListLengthReceiveTimerHostChecks
{
    public static async Task ResolvedBudgetExcludesGuardSuspension()
    {
        await using var runtime = new ProcessRuntime();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var cleanup = new RuntimeNodeFoundationHostActor(runtime);
        var queue = new ListLengthYieldContext();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<Task<ListLengthSelection?>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selected = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        Term input = Nil.Value;
        for (int index = 0; index < ListLengthHostNames.Cells; index++)
            input = new Cons(Nil.Value, input);
        var process = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            context.Mailbox.Send(input);

            async ValueTask<ListLengthSelection?> Select(Term message)
            {
                if (message is Cons)
                {
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ErlangModuleNames.Length,
                        message
                    );

                    return null;
                }

                return new ListLengthSelection(message);
            }
            Task<ListLengthSelection?> pending;
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(queue);
            try
            {
                // The finite TimeSpan path starts with an already resolved
                // budget. Reflection opens only this internal HOST seam.
                MethodInfo method = typeof(Mailbox).GetMethod(
                    ListLengthHostNames.ReceiveSelectingMethod,
                    BindingFlags.Instance | BindingFlags.NonPublic
                )!.MakeGenericMethod(typeof(ListLengthSelection));
                var operation = (ValueTask<ListLengthSelection?>)method.Invoke(
                    context.Mailbox,
                    [(Func<Term, ValueTask<ListLengthSelection?>>)Select, ListLengthHostNames.ReceiveTimeout, context.Cancellation, null]
                )!;
                pending = operation.AsTask();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
            entered.TrySetResult(pending);
            try
            {
                var result = await pending;
                if (result is null)
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                selected.TrySetResult(result.Value);

                return result.Value;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        ProcessContext running = await ready.Task.WaitAsync(ListLengthHostNames.Deadline);
        Task<ListLengthSelection?> operationTask = await entered.Task.WaitAsync(ListLengthHostNames.Deadline);
        try
        {
            if (operationTask.IsCompleted)
                throw new InvalidOperationException(ListLengthHostNames.Failure);
            var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<Term> suspended = observer.InvokeAsync(context =>
            {
                var request = runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessSuspensionNames.Suspend,
                    process.Pid
                ).AsTask();
                requested.TrySetResult();

                return request;
            });
            await requested.Task.WaitAsync(ListLengthHostNames.Deadline);
            await queue.ResumeUntil(suspended);
            if (!(await suspended.WaitAsync(ListLengthHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.True)))
                throw new InvalidOperationException(ListLengthHostNames.Failure);
            // A real timer deliberately exceeds the finite receive budget
            // while the target is acknowledged as suspended. This is a timer
            // contract, not a speed or reduction-counter comparison.
            await Task.Delay(ListLengthHostNames.SuspensionObservation);
            if (operationTask.IsCompleted)
                throw new InvalidOperationException(ListLengthHostNames.Failure);
            await observer.InvokeAsync(async context =>
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessSuspensionNames.Resume,
                    process.Pid
                ));
            running.Mailbox.Send(Term.A(ListLengthHostNames.Priority));
            await queue.ResumeUntil(operationTask);
            if (!(await selected.Task.WaitAsync(ListLengthHostNames.Deadline)).Equals(Term.A(ListLengthHostNames.Priority)))
                throw new InvalidOperationException(ListLengthHostNames.Failure);
        }
        finally
        {
            if (!operationTask.IsCompleted)
            {
                await cleanup.Invoke(context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ListLengthHostNames.Kill));

                    return true;
                });
                await queue.ResumeUntil(operationTask);
            }
            await process.Completion.WaitAsync(ListLengthHostNames.Deadline);
            await unwound.Task.WaitAsync(ListLengthHostNames.Deadline);
        }
    }
}
