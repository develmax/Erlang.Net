namespace Erlang.Tests;

public static partial class SourcePurgeStageHostChecks
{
    private static async Task Kill(ProcessRuntime runtime, ProcessHandle actor)
    {
        ProcessHandle killer = runtime.Spawn(context =>
        {
            runtime.Exit(context, actor.Pid, Term.A(ProcessExitSignals.Kill));

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await killer.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
    }

    private static Task<Term> Guest(Func<ValueTask<Term>> operation)
    {
        try
        {
            return operation().AsTask();
        }
        catch (ErlangException failure)
        {
            return Task.FromException<Term>(failure);
        }
    }

    private static async Task BadArgument(Task<Term> invocation)
    {
        try
        {
            await invocation.WaitAsync(SourcePurgeStageHostNames.Deadline);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }
        throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
    }

    private static async Task CancellationAndGuestPrecedence()
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        var announced = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                Task<bool> waiting = SourcePurgeStageHostBridge.Publish(runtime.Modules, context, () => Definition().Register(runtime.Modules)).AsTask();
                announced.TrySetResult(waiting);
                try
                {
                    await waiting;
                    throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
                }
                catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested) { }
                finished.TrySetResult();
            }
            catch (Exception failure)
            {
                announced.TrySetException(failure);
                finished.TrySetException(failure);
            }

            return Nil.Value;
        });
        try
        {
            Require(!(await announced.Task.WaitAsync(SourcePurgeStageHostNames.Deadline)).IsCompleted);
            await Kill(runtime, actor);
            await finished.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
            Require(runtime.Modules.PendingPurge(SourcePurgeStageHostBridge.Image(session)) is { IsCompleted: false });
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                Task<Term> invalidKeep = Guest(() => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    BeamLoaderAdmissionNames.FinishOnLoad,
                    Term.I(0),
                    Term.I(0)
                ));
                Require(invalidKeep.IsCompleted);
                await BadArgument(invalidKeep);
                Task<Term> invalidModule = Guest(() => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    BeamLoaderAdmissionNames.FinishOnLoad,
                    Term.I(0),
                    Term.A(ErlangBooleanAtoms.True)
                ));
                Require(!invalidModule.IsCompleted);
                SourcePurgeStageHostBridge.Finish(runtime.Modules, session);
                await BadArgument(invalidModule);
            }
            );
        }
        finally
        {
            SourcePurgeStageHostBridge.Finish(runtime.Modules, session);
        }
        await actor.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
        Definition().Register(runtime.Modules);
        await CancellationAfterStage(runtime);
    }

    private static async Task CancellationAfterStage(ProcessRuntime runtime)
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (IDisposable writer = await SourcePurgeStageHostBridge.CodeTicket(runtime.Modules))
        {
            ProcessHandle actor = runtime.Spawn(async context =>
            {
                try
                {
                    try
                    {
                        await SourcePurgeStageHostBridge.Publish(runtime.Modules, context, () => Definition(SourcePurgeStageHostNames.OtherSource).Register(runtime.Modules));
                        throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
                    }
                    catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested) { }
                    finished.TrySetResult();
                }
                catch (Exception failure)
                {
                    finished.TrySetException(failure);
                }

                return Nil.Value;
            });
            using var deadline = new CancellationTokenSource(SourcePurgeStageHostNames.Deadline);
            while ((int)SourcePurgeStageHostBridge.Property(SourcePurgeStageHostBridge.CodePermission(runtime.Modules), SourcePurgeStageHostNames.PendingWriters) == 0)
            {
                deadline.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            Refused(() => SourcePurgeStageHostBridge.SourceTicket(runtime.Modules), SourcePurgeStageHostNames.StageBusy);
            await Kill(runtime, actor);
            await finished.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
            await actor.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
            using var free = SourcePurgeStageHostBridge.SourceTicket(runtime.Modules);
        }
        Require(runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Other) is null);
        Definition(SourcePurgeStageHostNames.OtherSource).Register(runtime.Modules);
    }
}
