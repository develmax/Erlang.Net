using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class HaltLifecycleHostChecks
{
    public static async Task Run()
    {
        await NonReturn(false);
        await NonReturn(true);
        await InitializerAndElection();
        await HostCatchAndQuiescence();
        await BehaviourCallback();
        await HaltPortHostChecks.ImmediateEcho();
        await TimerAndBlockedReceive();
    }

    private static async Task NonReturn(bool compiled)
    {
        await using var survivor = new ProcessRuntime();
        await using var runtime = new ProcessRuntime();
        int continuations = 0;
        int handlers = 0;
        int afters = 0;
        ProcessContext? caller = null;
        for (int arity = 0; arity <= 2; arity++)
            Require(runtime.Modules.HasExport(ErlangModuleNames.Module, HaltLifecycleHostNames.Halt, arity));
        runtime.Modules.Register(
            HaltLifecycleHostNames.Module,
            HaltLifecycleHostNames.Stop,
            1,
            (context, arguments) =>
        {
            context.Halt((int)((Integer)arguments[0]).Value);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        runtime.Modules.Register(
            HaltLifecycleHostNames.Module,
            HaltLifecycleHostNames.Continuation,
            0,
            (context, arguments) =>
        {
            Interlocked.Increment(ref continuations);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        runtime.Modules.Register(
            HaltLifecycleHostNames.Module,
            HaltLifecycleHostNames.Catch,
            0,
            (context, arguments) =>
        {
            Interlocked.Increment(ref handlers);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        runtime.Modules.Register(
            HaltLifecycleHostNames.Module,
            HaltLifecycleHostNames.After,
            0,
            (context, arguments) =>
        {
            Interlocked.Increment(ref afters);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        using var generated = compiled ? GeneratedModuleCompiler.Compile(HaltLifecycleHostNames.Source) : null;
        if (generated is not null)
            generated.Definition.Register(runtime.Modules);
        else
            new Parser(HaltLifecycleHostNames.Source).ParseModule().Register(runtime.Modules);
        ProcessHandle process = runtime.Spawn(context =>
        {
            caller = context;

            return runtime.Modules.Call(context, HaltLifecycleHostNames.Driver, HaltLifecycleHostNames.Run);
        });
        RuntimeHaltResult result = await runtime.HaltCompletion.WaitAsync(HaltLifecycleHostNames.Timeout);
        Require(result.Status == HaltLifecycleHostNames.TransferStatus);
        Require((await process.Completion.WaitAsync(HaltLifecycleHostNames.Timeout)).Equals(Term.A(HaltLifecycleHostNames.Shutdown)));
        await runtime.HaltQuiescence.WaitAsync(HaltLifecycleHostNames.Timeout);
        Require(runtime.Lifecycle == RuntimeLifecycleState.Stopped);
        Require(runtime.ProcessSnapshot().Count == 0);
        Require(continuations == 0 && handlers == 0 && afters == 0);
        caller!.Mailbox.Send(Nil.Value);
        Require(caller.Mailbox.Count == 0);
        Throws<ObjectDisposedException>(() => runtime.Spawn(context => ValueTask.FromResult<Term>(Nil.Value)));
        Throws<ObjectDisposedException>(() => runtime.Send(process.Pid, Nil.Value));
        Throws<ObjectDisposedException>(() => runtime.InitializeModulesAsync(_ => Interlocked.Increment(ref continuations)));
        Throws<ProcessHaltTransfer>(() => runtime.Modules.Call(caller!, HaltLifecycleHostNames.Module, HaltLifecycleHostNames.Continuation));
        Require(continuations == 0);
        Term? survived = null;
        ProcessHandle live = survivor.Spawn(async context =>
        {
            survivor.Send(context.Self, Term.I(HaltLifecycleHostNames.TransferStatus));
            survived = await context.ReceiveAsync<Term>(value => value, HaltLifecycleHostNames.Timeout);

            return Nil.Value;
        });
        Require((await live.Completion.WaitAsync(HaltLifecycleHostNames.Timeout)).Equals(Term.A(HaltLifecycleHostNames.Normal)));
        Require(survived!.Equals(Term.I(HaltLifecycleHostNames.TransferStatus)));
        Require(survivor.Lifecycle == RuntimeLifecycleState.Running);
    }

    private static async Task InitializerAndElection()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => runtime.RequestImmediateHalt(HaltLifecycleHostNames.InitializerStatus));
        var attempts = Enumerable.Range(0, HaltLifecycleHostNames.Contenders).Select(status => Task.Run(() => runtime.RequestImmediateHalt(status))).ToArray();
        foreach (RuntimeHaltResult result in await Task.WhenAll(attempts).WaitAsync(HaltLifecycleHostNames.Timeout))
            Require(result.Status == HaltLifecycleHostNames.InitializerStatus);
        await runtime.HaltQuiescence.WaitAsync(HaltLifecycleHostNames.Timeout);
        Throws<ArgumentOutOfRangeException>(() => runtime.RequestImmediateHalt(-1));
        Require((await runtime.HaltCompletion).Status == HaltLifecycleHostNames.InitializerStatus);
    }

    private static async Task HostCatchAndQuiescence()
    {
        await using var runtime = new ProcessRuntime();
        var caught = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int bodyCalls = 0;
        runtime.Modules.Register(
            HaltLifecycleHostNames.Module,
            HaltLifecycleHostNames.Continuation,
            0,
            (context, arguments) =>
        {
            Interlocked.Increment(ref bodyCalls);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        ProcessHandle process = runtime.Spawn(async context =>
        {
            using var cancellation = context.Cancellation.Register(() =>
            {
                Task.Run(() => runtime.RequestImmediateHalt(0)).WaitAsync(HaltLifecycleHostNames.Timeout).GetAwaiter().GetResult();
                callback.SetResult();
            });
            try
            {
                context.Halt(HaltLifecycleHostNames.SwallowedTransferStatus);
            }
            catch (ProcessHaltTransfer)
            {
                caught.SetResult();
            }
            Throws<ProcessHaltTransfer>(() => runtime.Modules.Call(context, HaltLifecycleHostNames.Module, HaltLifecycleHostNames.Continuation));
            await release.Task;

            return Nil.Value;
        });
        await caught.Task.WaitAsync(HaltLifecycleHostNames.Timeout);
        Require((await runtime.HaltCompletion).Status == HaltLifecycleHostNames.SwallowedTransferStatus);
        Require(!runtime.HaltQuiescence.IsCompleted);
        Require(!runtime.IsAlive(process.Pid));
        await callback.Task.WaitAsync(HaltLifecycleHostNames.Timeout);
        release.SetResult();
        await runtime.HaltQuiescence.WaitAsync(HaltLifecycleHostNames.Timeout);
        Require(bodyCalls == 0);
    }

    private static async Task BehaviourCallback()
    {
        await using var runtime = new ProcessRuntime();
        var server = new HaltLifecycleServer();
        ProcessHandle process = await Erlang.Otp.GenServer.Start(runtime, server, Nil.Value);
        runtime.Send(process.Pid, Term.Tuple(Term.A(HaltLifecycleHostNames.Cast), Nil.Value));
        Require((await runtime.HaltCompletion.WaitAsync(HaltLifecycleHostNames.Timeout)).Status == HaltLifecycleHostNames.TransferStatus);
        await runtime.HaltQuiescence.WaitAsync(HaltLifecycleHostNames.Timeout);
        Require(server.Terminations == 0 && server.Continuations == 0);
        Require((await process.Completion).Equals(Term.A(HaltLifecycleHostNames.Shutdown)));
    }

    private static async Task TimerAndBlockedReceive()
    {
        await using var runtime = new ProcessRuntime();
        var contextReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            contextReady.SetResult(context);
            await context.ReceiveAsync<Term>(value =>
            {
                if (!value.Equals(Term.A(HaltLifecycleHostNames.HeldMessage)))
                    return null;
                entered.SetResult();
                release.Task.GetAwaiter().GetResult();

                return value;
            });
            await stopped.Task.WaitAsync(context.Cancellation);

            return Nil.Value;
        });
        ProcessContext captured = await contextReady.Task.WaitAsync(HaltLifecycleHostNames.Timeout);
        await runtime.Modules.Call(
            captured,
            ErlangModuleNames.Module,
            HaltLifecycleHostNames.TimerStart,
            Term.I(HaltLifecycleHostNames.PendingTimerMilliseconds),
            owner.Pid,
            Term.A(HaltLifecycleHostNames.TimerMessage)
        );
        runtime.Send(owner.Pid, Term.A(HaltLifecycleHostNames.HeldMessage));
        await entered.Task.WaitAsync(HaltLifecycleHostNames.Timeout);
        await runtime.Modules.Call(
            captured,
            ErlangModuleNames.Module,
            HaltLifecycleHostNames.TimerStart,
            Term.I(0),
            owner.Pid,
            Term.A(HaltLifecycleHostNames.TimerMessage)
        );
        Task<RuntimeHaltResult> halt = Task.Run(() => runtime.RequestImmediateHalt(HaltLifecycleHostNames.TransferStatus));
        try
        {
            Require(!runtime.HaltQuiescence.IsCompleted);
        }
        finally
        {
            release.SetResult();
        }
        Require((await halt.WaitAsync(HaltLifecycleHostNames.Timeout)).Status == HaltLifecycleHostNames.TransferStatus);
        await runtime.HaltQuiescence.WaitAsync(HaltLifecycleHostNames.Timeout);
        Require(captured.Mailbox.Count == 0);
        captured.Mailbox.Send(Term.A(HaltLifecycleHostNames.TimerMessage));
        Require(captured.Mailbox.Count == 0);
        Require(!runtime.IsAlive(owner.Pid));
    }

    private static Term Text(string value) => Cons.From(value.Select(character => Term.I(character)));

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(HaltLifecycleHostNames.Failure);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HaltLifecycleHostNames.Failure);
    }
}
