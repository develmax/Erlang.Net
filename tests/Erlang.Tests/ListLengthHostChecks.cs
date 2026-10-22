using Erlang.Differential;

namespace Erlang.Tests;

public static class ListLengthHostChecks
{
    public static Task ParseAll()
    {
        foreach (var row in ListLengthCases.Modules)
            new Erlang.Compiler.Parser(row.Source).ParseModule();
        new Erlang.Compiler.Parser(ListLengthHostNames.Source).ParseModule();

        return Task.CompletedTask;
    }

    public static Task OrdinaryProgress() => Boundary(null, false);

    public static Task OrdinaryCancellation() => Boundary(null, true);

    public static Task GuardProgress() => Boundary(ListLengthHostNames.Run, false);

    public static Task GuardCancellation() => Boundary(ListLengthHostNames.Run, true);

    public static Task ReceiveProgress() => Boundary(ListLengthHostNames.Receive, false);

    public static Task ReceiveCancellation() => Boundary(ListLengthHostNames.Receive, true);

    public static Task DynamicMapKeyProgress() => Boundary(ListLengthHostNames.MapKey, false);

    public static Task DynamicBitSizeProgress() => Boundary(ListLengthHostNames.BitSize, false);

    public static Task ReceivePriorityRevalidation() => Boundary(ListLengthHostNames.Receive, false, priority: true);

    public static Task ReceiveCandidateRemoval() => Boundary(ListLengthHostNames.Receive, false, removal: true);

    public static Task OrdinarySuspendResume() => Boundary(null, false, suspend: true);

    public static Task GuardSuspendResume() => Boundary(ListLengthHostNames.Run, false, suspend: true);

    public static Task ReceiveSuspendResume() => Boundary(ListLengthHostNames.Receive, false, suspend: true);

    public static Task OrdsetSizeProgress() => Boundary(null, false, ordset: true);

    public static Task OrdsetSizeCancellation() => Boundary(null, true, ordset: true);

    public static Task OrdsetSizeSuspendResume() => Boundary(
        null,
        false,
        suspend: true,
        ordset: true
    );

    public static async Task LateImproperOriginalArgument()
    {
        foreach (bool ordset in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await using var observer = new RuntimeNodeFoundationHostActor(runtime);
            var queue = new ListLengthYieldContext();
            var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Term original = Term.A(ListLengthHostNames.ImproperTail);
            for (int index = 0; index < ListLengthHostNames.Cells; index++)
                original = new Cons(new MapTerm([]), original);
            Task<Term> invocation = actor.InvokeAsync(async context =>
            {
                var previous = SynchronizationContext.Current;
                Task<Term> operation;
                SynchronizationContext.SetSynchronizationContext(queue);
                try
                {
                    operation = runtime.Modules.Call(
                        context,
                        ordset ? ListLengthHostNames.Ordsets : ErlangModuleNames.Module,
                        ordset ? ListLengthHostNames.Size : ErlangModuleNames.Length,
                        original
                    ).AsTask();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previous);
                }
                entered.TrySetResult(operation);

                return await operation;
            });
            Task<Term> pending = await entered.Task.WaitAsync(ListLengthHostNames.Deadline);
            try
            {
                if (pending.IsCompleted || !await observer.Invoke(_ => true))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                await queue.ResumeUntil(pending);
                ErlangException? error = null;
                try
                {
                    await invocation;
                }
                catch (ErlangException failure)
                {
                    error = failure;
                }
                if (error is null || error.ExceptionClass != ErlangExceptionClasses.Error || !error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
                if (frames.Length == 0 || frames[0] is not TupleTerm { Items.Count: 4 } first
                    || !first.Items[0].Equals(Term.A(ErlangModuleNames.Module))
                    || !first.Items[1].Equals(Term.A(ErlangModuleNames.Length))
                    || first.Items[2] is not Cons { Tail: Nil } args
                    || !ReferenceEquals(args.Head, original))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                if (ordset && (frames.Length < 2 || frames[1] is not TupleTerm { Items.Count: 4 } wrapper
                    || !wrapper.Items[0].Equals(Term.A(ListLengthHostNames.Ordsets))
                    || !wrapper.Items[1].Equals(Term.A(ListLengthHostNames.Size))))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                Term followup = await actor.InvokeAsync(context => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.Length,
                    Nil.Value
                ).AsTask());
                if (!followup.Equals(Term.I(0)))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
            }
            finally
            {
                if (!pending.IsCompleted)
                {
                    await observer.Invoke(context =>
                    {
                        runtime.Exit(context, actor.Process.Pid, Term.A(ListLengthHostNames.Kill));

                        return true;
                    });
                    await queue.ResumeUntil(pending);
                }
            }
        }
    }

    private static async Task Boundary(
        string? function,
        bool cancel,
        bool priority = false,
        bool suspend = false,
        bool removal = false,
        bool ordset = false
    )
    {
        using var compiled = GeneratedModuleCompiler.Compile(ListLengthHostNames.Source);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var cleanup = new RuntimeNodeFoundationHostActor(runtime);
        var queue = new ListLengthYieldContext();
        var contextReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var returned = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        Term input = Nil.Value;
        for (int index = 0; index < ListLengthHostNames.Cells; index++)
            input = new Cons(Nil.Value, input);
        Term[] arguments = function switch
        {
            ListLengthHostNames.MapKey => [input, new MapTerm([new(Term.I(ListLengthHostNames.Cells), Term.A(ListLengthHostNames.Ok))])],
            ListLengthHostNames.BitSize => [input, new BitString(
                new byte[(ListLengthHostNames.Cells + ListLengthHostNames.TrailingBits + ListLengthHostNames.ByteRounding) / ListLengthHostNames.BitsPerByte],
                ListLengthHostNames.Cells + ListLengthHostNames.TrailingBits
            )],
            _ => [input]
        };
        var process = runtime.Spawn(async context =>
        {
            contextReady.TrySetResult(context);
            Task<Term> operation;
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(queue);
            try
            {
                operation = runtime.Modules.Call(
                    context,
                    ordset ? ListLengthHostNames.Ordsets : function is not null ? ListLengthHostNames.Module : ErlangModuleNames.Module,
                    ordset ? ListLengthHostNames.Size : function ?? ErlangModuleNames.Length,
                    arguments
                ).AsTask();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
            entered.TrySetResult(operation);
            try
            {
                Term result = await operation;
                returned.TrySetResult(result);

                return result;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        Task<Term> pending = await entered.Task.WaitAsync(ListLengthHostNames.Deadline);
        ProcessContext running = await contextReady.Task.WaitAsync(ListLengthHostNames.Deadline);
        try
        {
            if (pending.IsCompleted || unwound.Task.IsCompleted)
                throw new InvalidOperationException(ListLengthHostNames.Failure);
            // The second real actor progresses while the length operation is
            // suspended at its actual cooperative boundary, before traversal ends.
            bool progressed = await observer.Invoke(context =>
            {
                if (cancel)
                    runtime.Exit(context, process.Pid, Term.A(ListLengthHostNames.Kill));
                if (priority)
                    running.Mailbox.Send(Term.A(ListLengthHostNames.Priority), priority: true);
                if (removal)
                {
                    if (!running.Mailbox.RemoveFirst(value => value is Cons))
                        throw new InvalidOperationException(ListLengthHostNames.Failure);
                    running.Mailbox.Send(Term.A(ListLengthHostNames.Priority));
                }

                return true;
            });
            if (!progressed || pending.IsCompleted)
                throw new InvalidOperationException(ListLengthHostNames.Failure);
            if (suspend)
            {
                var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<Term> suspended = observer.InvokeAsync(context =>
                {
                    Task<Term> request = runtime.Modules.Call(
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
                if (!(await suspended.WaitAsync(ListLengthHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.True)) || pending.IsCompleted)
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                Term resumed = await observer.InvokeAsync(async context =>
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessSuspensionNames.Resume,
                        process.Pid
                    ));
                if (!resumed.Equals(Term.A(ErlangBooleanAtoms.True)) || pending.IsCompleted)
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
            }
            await queue.ResumeUntil(pending);
            await process.Completion.WaitAsync(ListLengthHostNames.Deadline);
            await unwound.Task.WaitAsync(ListLengthHostNames.Deadline);
            if (cancel)
            {
                if (!pending.IsCanceled || returned.Task.IsCompleted)
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                Term reason = await process.Completion;
                if (!reason.Equals(Term.A(ListLengthHostNames.Killed)))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
            }
            else
            {
                Term expected = removal
                    ? Term.Tuple(Term.A(ListLengthHostNames.Priority), Term.A(ListLengthHostNames.Lost))
                    : priority
                    ? Term.Tuple(Term.A(ListLengthHostNames.Priority), Term.A(ListLengthHostNames.Retained))
                    : function is not null ? Term.A(ListLengthHostNames.Ok) : Term.I(ListLengthHostNames.Cells);
                if (!(await returned.Task.WaitAsync(ListLengthHostNames.Deadline)).Equals(expected))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
            }
        }
        finally
        {
            if (!pending.IsCompleted)
            {
                await cleanup.Invoke(context =>
                {
                    runtime.Exit(context, process.Pid, Term.A(ListLengthHostNames.Kill));

                    return true;
                });
                await queue.ResumeUntil(pending);
            }
            await process.Completion.WaitAsync(ListLengthHostNames.Deadline);
            await unwound.Task.WaitAsync(ListLengthHostNames.Deadline);
        }
    }
}
