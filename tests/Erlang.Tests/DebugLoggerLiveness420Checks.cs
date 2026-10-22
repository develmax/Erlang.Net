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
        await using var victim = new RuntimeNodeFoundationHostActor(runtime);
        await using var logger = new RuntimeNodeFoundationHostActor(runtime);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        runtime.Register(DebugLoggerLiveness420Names.Logger, logger.Process.Pid);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? held = null;
        try
        {
            held = victim.InvokeAsync(async context =>
         {
             await DirtyNative419Access.Schedule(
                 context,
                 DirtyNative419Names.Cpu,
                 [],
                 (_, _) =>
    {
        entered.Set();
        DebugInternal420Access.Require(release.Wait(DebugInternal420Names.Deadline));

        return Nil.Value;
    }
             );

             return true;
         });
            DebugInternal420Access.Require(entered.Wait(DebugInternal420Names.Deadline));
            ProcessContext target = await victim.Ready;
            // This real canceled callback is still physically held and still in the
            // process registry: the old dictionary-only predicate would deliver to it.
            DebugLocks419Access.Cancel(target);
            DebugInternal420Access.Require(runtime.IsAlive(victim.Process.Pid) && !victim.Process.Completion.IsCompleted);
            await Send(caller, victim.Process.Pid);
            await Receive(logger, caller.Process.Pid);
            await caller.Invoke(context =>
         {
             runtime.Exit(context, victim.Process.Pid, Term.A(DebugLoggerLiveness420Names.Kill));

             return true;
         });
            // This second case is actual owner termination with native work retaining
            // the pre-DOWN/Done barrier. A dead explicit logger PID is still accepted
            // as configuration but delivery must reset it to the named logger.
            DebugInternal420Access.Require(!runtime.IsAlive(victim.Process.Pid) && !victim.Process.Completion.IsCompleted);
            await Send(caller, victim.Process.Pid);
            await Receive(logger, caller.Process.Pid);
        }
        finally
        {
            release.Set();
            await SettleHeld(victim, held);
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
        Task<bool>? held = null;
        try
        {
            held = logger.InvokeAsync(async context =>
         {
             await DirtyNative419Access.Schedule(
                 context,
                 DirtyNative419Names.Io,
                 [],
                 (_, _) =>
    {
        entered.Set();
        DebugInternal420Access.Require(release.Wait(DebugInternal420Names.Deadline));

        return Nil.Value;
    }
             );

             return true;
         });
            DebugInternal420Access.Require(entered.Wait(DebugInternal420Names.Deadline));
            DebugLocks419Access.Cancel(await logger.Ready);
            DebugInternal420Access.Require(runtime.IsAlive(logger.Process.Pid) && !logger.Process.Completion.IsCompleted);
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
            await SettleHeld(logger, held);
        }
    }

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
