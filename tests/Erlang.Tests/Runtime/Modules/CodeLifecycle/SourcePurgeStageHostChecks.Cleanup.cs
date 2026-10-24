namespace Erlang.Tests;

public static partial class SourcePurgeStageHostChecks
{
    private static async Task HardPurgeCleanup(bool killRequester)
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Module)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            SourcePurgeStageHostNames.Callback,
            SourcePurgeStageHostNames.Block,
            0,
            async (context, _) =>
        {
            entered.TrySetResult(context);
            try
            {
                return (await context.ReceiveAsync<Term>(message => message))!;
            }
            finally
            {
                await Task.Run(() => runtime.Modules.Apply(registry =>
                {
                    registry.Register(
                        SourcePurgeStageHostNames.Clean,
                        SourcePurgeStageHostNames.Value,
                        0,
                        (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                    );
                    cleanup.TrySetResult();
                }));
                await release.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
            }
        }
        );
        ProcessHandle target = runtime.Spawn(context => runtime.Modules.Call(context, SourcePurgeStageHostNames.Module, SourcePurgeStageHostNames.Run));
        ProcessContext executing = await entered.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
        Require((bool)SourcePurgeStageHostBridge.Call(executing, SourcePurgeStageHostNames.IsExecutingCode, image)!);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        var announced = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var purgeResult = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle requester = runtime.Spawn(async context =>
        {
            Term returned = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                CodePurgeHostNames.Purge,
                Term.A(SourcePurgeStageHostNames.Module)
            );
            purgeResult.TrySetResult(returned);

            return Nil.Value;
        });
        ProcessHandle? publisher = null;
        try
        {
            await cleanup.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
            Require(runtime.Modules.PendingPurge(image) is { IsCompleted: false });
            Require(!SourcePurgeStageHostBridge.Unwound(executing).IsCompleted);
            Require(runtime.Modules.HasExport(SourcePurgeStageHostNames.Clean, SourcePurgeStageHostNames.Value, 0));
            Refused(() => Definition().Register(runtime.Modules), SourcePurgeStageHostNames.StageBusy);
            publisher = runtime.Spawn(async context =>
            {
                try
                {
                    Task<bool> waiting = SourcePurgeStageHostBridge.Publish(runtime.Modules, context, () => Definition().Register(runtime.Modules)).AsTask();
                    announced.TrySetResult(waiting);
                    Require(await waiting);
                    published.TrySetResult();
                }
                catch (Exception failure)
                {
                    announced.TrySetException(failure);
                    published.TrySetException(failure);
                }

                return Nil.Value;
            });
            Require(!(await announced.Task.WaitAsync(SourcePurgeStageHostNames.Deadline)).IsCompleted);
            if (killRequester)
                await Kill(runtime, requester);
        }
        finally
        {
            release.TrySetResult();
        }
        await SourcePurgeStageHostBridge.Unwound(executing).WaitAsync(SourcePurgeStageHostNames.Deadline);
        Require((await target.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Killed)));
        await published.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
        await publisher!.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
        Term result = await requester.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
        Require(result.Equals(Term.A(killRequester ? ProcessExitReasons.Killed : ProcessExitReasons.Normal)));
        if (!killRequester)
            Require((await purgeResult.Task.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(Term.A(ErlangBooleanAtoms.True)));
        Require(!runtime.Modules.HasOldCode(SourcePurgeStageHostNames.Module) && runtime.Modules.PendingPurge(image) is null);
    }

    private static async Task CloseSessions(bool halt)
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        ModuleCodeImage image = SourcePurgeStageHostBridge.Image(session);
        runtime.Modules.Apply(_ =>
        {
            Refused(() => runtime.DisposeAsync().AsTask().GetAwaiter().GetResult(), SourcePurgeStageHostNames.ReentrantShutdown);
            Refused(() => runtime.RequestImmediateHalt(0).GetAwaiter().GetResult(), SourcePurgeStageHostNames.ReentrantShutdown);
        });
        Require(runtime.Modules.PendingPurge(image) is { IsCompleted: false });
        var announced = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            Task<bool> waiting = SourcePurgeStageHostBridge.Publish(runtime.Modules, context, () => Definition().Register(runtime.Modules)).AsTask();
            announced.TrySetResult(waiting);
            await waiting;
            throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
        });
        Require(!(await announced.Task.WaitAsync(SourcePurgeStageHostNames.Deadline)).IsCompleted);
        if (halt)
        {
            await runtime.RequestImmediateHalt(0).WaitAsync(SourcePurgeStageHostNames.Deadline);
            await runtime.HaltQuiescence.WaitAsync(SourcePurgeStageHostNames.Deadline);
        }
        else
            await runtime.DisposeAsync().AsTask().WaitAsync(SourcePurgeStageHostNames.Deadline);
        Require((await actor.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Shutdown)));
        Require(runtime.Modules.PendingPurge(image) is null && runtime.Modules.HasOldCode(SourcePurgeStageHostNames.Module));
        Require(!SourcePurgeStageHostBridge.Finish(runtime.Modules, session));
        Refused(() => Definition().Register(runtime.Modules), SourcePurgeStageHostNames.RuntimeClosing);
    }
}
