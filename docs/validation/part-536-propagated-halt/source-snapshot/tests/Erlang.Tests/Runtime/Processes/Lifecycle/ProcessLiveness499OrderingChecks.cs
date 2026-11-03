namespace Erlang.Tests;

internal static class ProcessLiveness499OrderingChecks
{
    internal static async Task Run()
    {
        await Run(ProcessLiveness499Names.Kill, false);
        await Run(ProcessLiveness499Names.Normal, false);
        await Run(ProcessLiveness499Names.Normal, true);
    }

    private static async Task Run(string reason, bool cancel)
    {
        await using var runtime = new ProcessRuntime(2);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext child = await target.Ready;
        await caller.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ProcessLiveness499Names.Erlang,
            ProcessLiveness499Names.ProcessFlag,
            Term.A(ProcessLiveness499Names.Scheduler),
            Term.I(1)
        ));
        await target.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ProcessLiveness499Names.Erlang,
            ProcessLiveness499Names.ProcessFlag,
            Term.A(ProcessLiveness499Names.Scheduler),
            Term.I(2)
        ));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> held = target.InvokeAsync(async context =>
        {
            entered.Set();
            if (!release.Wait(ProcessLiveness499Names.Deadline))
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            await ReceiveForOrdering(runtime, context, context.Cancellation);

            return true;
        });
        Task<Term>? query = null;
        try
        {
            if (!entered.Wait(ProcessLiveness499Names.Deadline))
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            query = caller.InvokeAsync(async context =>
            {
                await runtime.Modules.Call(
                    context,
                    ProcessLiveness499Names.Erlang,
                    ProcessLiveness499Names.Exit,
                    child.Self,
                    Term.A(reason)
                );
                ValueTask<Term> result = runtime.Modules.Call(
                    context,
                    ProcessLiveness499Names.Erlang,
                    ProcessLiveness499Names.Alive,
                    child.Self
                );
                sent.TrySetResult();

                return await result;
            });
            await sent.Task.WaitAsync(ProcessLiveness499Names.Deadline);
            await Task.WhenAny(query, Task.Delay(ProcessLiveness499Names.PendingWindow));
            if (cancel)
            {
                if (query.IsCompleted)
                    throw new InvalidOperationException(ProcessLiveness499Names.Failure);
                runtime.Exit(child, caller.Process.Pid, Term.A(ProcessLiveness499Names.Kill));
                bool canceled = false;
                try
                {
                    await query.WaitAsync(ProcessLiveness499Names.Deadline);
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                if (!canceled || !(await caller.Process.Completion.WaitAsync(ProcessLiveness499Names.Deadline)).Equals(Term.A(ProcessLiveness499Names.Killed)))
                    throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            }
            release.Set();
            if (!cancel)
            {
                Term result = await query.WaitAsync(ProcessLiveness499Names.Deadline);
                if (!result.Equals(Term.A(reason == ProcessLiveness499Names.Kill ? ErlangBooleanAtoms.False : ErlangBooleanAtoms.True)))
                    throw new InvalidOperationException(ProcessLiveness499Names.Failure);
            }
            if (reason != ProcessLiveness499Names.Kill)
                runtime.Exit(child, child.Self, Term.A(ProcessLiveness499Names.Kill));
            if (!(await target.Process.Completion.WaitAsync(ProcessLiveness499Names.Deadline)).Equals(Term.A(ProcessLiveness499Names.Killed)))
                throw new InvalidOperationException(ProcessLiveness499Names.Failure);
        }
        finally
        {
            release.Set();
            await held;
            if (query is not null && !cancel)
                await query.WaitAsync(ProcessLiveness499Names.Deadline);
        }
    }

    internal static async Task ReceiveForOrdering(
        ProcessRuntime runtime,
        ProcessContext context,
        CancellationToken cancellation,
        Action<bool>? observe = null
    )
    {
        try
        {
            await context.Mailbox.ReceiveAsync<Term>(_ => null, TimeSpan.Zero, cancellation);
        }
        catch (OperationCanceledException) when (TerminalCancellation(runtime, context, observe)) { }
    }

    private static bool TerminalCancellation(ProcessRuntime runtime, ProcessContext context, Action<bool>? observe)
    {
        bool requested = context.Cancellation.IsCancellationRequested;
        observe?.Invoke(requested);

        return requested || !runtime.IsAlive(context.Self);
    }
}
