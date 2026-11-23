using System.Text;
namespace Erlang.Tests;

internal static class DebugLoggerLiveness420Checks
{
    internal static async Task Run()
    {
        await Explicit();
        await Named();
    }

    private static async Task Explicit()
    {
        await using var runtime = new ProcessRuntime();
        await DebugInternal420Access.Initialize(runtime);
        await using var logger = new RuntimeNodeFoundationHostActor(runtime);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        runtime.Register(DebugLoggerLiveness420Names.Logger, logger.Process.Pid);
        await CheckExplicitRecipient(
            runtime,
            caller,
            logger,
            retiring: false
        );
        await CheckExplicitRecipient(
            runtime,
            caller,
            logger,
            retiring: true
        );
    }

    private static async Task CheckExplicitRecipient(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor caller,
        RuntimeNodeFoundationHostActor logger,
        bool retiring
    )
    {
        await using var victim = new RuntimeNodeFoundationHostActor(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var physicalReturn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>? held = null;
        try
        {
            held = HoldNative(
                victim,
                DirtyNative419Names.Cpu,
                entered,
                release,
                physicalReturn,
                retiring ? null : (stopped, resume.Task)
            );
            DebugInternal420Access.Require(entered.Wait(DebugInternal420Names.Deadline));

            if (retiring)
            {
                // This fresh owner reaches actual logical termination with its
                // dirty callback still blocked and host completion pending.
                await caller.Invoke(context =>
                {
                    runtime.Exit(context, victim.Process.Pid, Term.A(DebugLoggerLiveness420Names.Kill));

                    return true;
                });
                DebugInternal420Access.Require(SpinWait.SpinUntil(
                    () => !runtime.IsAlive(victim.Process.Pid),
                    DebugInternal420Names.Deadline
                ));
            }
            else
            {
                DebugLocks419Access.Cancel(await victim.Ready);
                await stopped.Task.WaitAsync(DebugInternal420Names.Deadline);
            }

            DebugInternal420Access.Require(runtime.IsAlive(victim.Process.Pid) == !retiring
                && !victim.Process.Completion.IsCompleted && !physicalReturn.Task.IsCompleted);
            await Send(caller, victim.Process.Pid);
            await Receive(logger, caller.Process.Pid);
        }
        finally
        {
            release.Set();
            resume.TrySetResult();
            await SettleHeld(victim, held);
        }

        if (retiring)
        {
            Term reason = await victim.Process.Completion.WaitAsync(DebugInternal420Names.Deadline);
            DebugInternal420Access.Require(reason.Equals(Term.A(DebugLoggerLiveness420Names.Killed)));
        }
    }

    private static async Task Send(RuntimeNodeFoundationHostActor caller, Pid target)
    {
        await caller.InvokeAsync(async context =>
        {
            await context.Runtime.Modules.Call(
                context,
                DebugInternal420Names.Runtime,
                DebugInternal420Names.Flag,
                Term.A(DebugInternal420Names.Logger),
                target
            );
            await DebugInternal420Access.Enabled(context, true);
            Term selected = await context.Runtime.Modules.Call(
                context,
                DebugInternal420Names.Runtime,
                DebugLoggerLiveness420Names.SystemInfo,
                Term.A(DebugInternal420Names.Logger)
            );
            DebugInternal420Access.Require(selected.Equals(Term.A(DebugLoggerLiveness420Names.Logger)));
            await DebugInternal420Access.Enabled(context, false);

            return true;
        });
    }

    private static Task<bool> Receive(RuntimeNodeFoundationHostActor logger, Pid sender) => logger.InvokeAsync(async context =>
    {
        Term? notice = await context.ReceiveAsync(message => message, DebugInternal420Names.Deadline);
        DebugInternal420Access.Require(notice is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(Term.A(DebugInternal420Names.Log)) && tuple.Items[4] is MapTerm { Entries.Count: 4 } metadata && metadata.Get(Term.A(DebugInternal420Names.Pid)).Equals(sender));

        return true;
    });

    private static async Task Named()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(streams);
        await DebugInternal420Access.Initialize(runtime);
        await using var logger = new RuntimeNodeFoundationHostActor(runtime);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        runtime.Register(DebugLoggerLiveness420Names.Logger, logger.Process.Pid);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var physicalReturn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>? held = null;
        try
        {
            held = HoldNative(
                logger,
                DirtyNative419Names.Io,
                entered,
                release,
                physicalReturn,
                (stopped, resume.Task)
            );
            DebugInternal420Access.Require(entered.Wait(DebugInternal420Names.Deadline));
            DebugLocks419Access.Cancel(await logger.Ready);
            await stopped.Task.WaitAsync(DebugInternal420Names.Deadline);
            DebugInternal420Access.Require(runtime.IsAlive(logger.Process.Pid)
                && !logger.Process.Completion.IsCompleted && !physicalReturn.Task.IsCompleted);
            await caller.InvokeAsync(async context =>
            {
                await DebugInternal420Access.Enabled(context, true);
                await DebugInternal420Access.Enabled(context, false);

                return true;
            });
            DebugInternal420Access.Require(Encoding.UTF8.GetString(error.ToArray()).Contains(DebugLoggerLiveness420Names.Prefix, StringComparison.Ordinal));
        }
        finally
        {
            release.Set();
            resume.TrySetResult();
            await SettleHeld(logger, held);
        }
    }

    private static Task<bool> HoldNative(
        RuntimeNodeFoundationHostActor actor,
        string kind,
        ManualResetEventSlim entered,
        ManualResetEventSlim release,
        TaskCompletionSource physicalReturn,
        (TaskCompletionSource Stopped, Task Resume)? keepRegistered
    ) => actor.InvokeAsync(async context =>
    {
        try
        {
            await DirtyNative419Access.Schedule(
                context,
                kind,
                [],
                (_, _) =>
                {
                    entered.Set();
                    try
                    {
                        DebugInternal420Access.Require(release.Wait(DebugInternal420Names.Deadline));

                        return Nil.Value;
                    }
                    finally
                    {
                        physicalReturn.TrySetResult();
                    }
                }
            );
        }
        catch (OperationCanceledException) when (keepRegistered is { } barrier && context.Cancellation.IsCancellationRequested)
        {
            // The cancellation interrupted the normal owner await, not the
            // physical callback. Keep this command inside the actor until the
            // logger observation finishes, before the canceled channel unwinds.
            barrier.Stopped.TrySetResult();
            await barrier.Resume.WaitAsync(DebugInternal420Names.Deadline);

            throw;
        }

        return true;
    });

    private static async Task SettleHeld(RuntimeNodeFoundationHostActor actor, Task<bool>? held)
    {
        if (held is null) return;
        ProcessContext context = await actor.Ready;
        try
        {
            await held;
        }
        catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
        {
            // The test explicitly canceled this owner while native work was held.
            // Its invocation may observe cancellation before the physical exit.
        }
        if (context.Cancellation.IsCancellationRequested)
            await actor.Process.Completion.WaitAsync(DebugInternal420Names.Deadline);
    }
}
