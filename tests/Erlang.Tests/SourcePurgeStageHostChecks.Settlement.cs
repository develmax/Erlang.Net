namespace Erlang.Tests;

public static partial class SourcePurgeStageHostChecks
{
    private static async Task SettlementReleasesWriter()
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        Task pending = runtime.Modules.PendingPurge(SourcePurgeStageHostBridge.Image(session))!;
        Task completion = ((TaskCompletionSource<bool>)SourcePurgeStageHostBridge.Property(session, SourcePurgeStageHostNames.Completion)).Task;
        IDisposable? writer = await SourcePurgeStageHostBridge.CodeTicket(runtime.Modules);
        Task<bool>? settlement = null;
        try
        {
            SourcePurgeStageHostBridge.WithCodeTicket(
                writer,
                () =>
            {
                Refused(() => SourcePurgeStageHostBridge.Finish(runtime.Modules, session), SourcePurgeStageHostNames.WriterOrder);

                return true;
            }
            );
            settlement = SourcePurgeStageHostBridge.FinishAsync(runtime.Modules, session).AsTask();
            Require(!settlement.IsCompleted && !pending.IsCompleted && !completion.IsCompleted);
            Refused(() => Definition().Register(runtime.Modules), SourcePurgeStageHostNames.StageBusy);
            writer.Dispose();
            writer = null;
            await completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
            Require((bool)SourcePurgeStageHostBridge.Property(SourcePurgeStageHostBridge.CodePermission(runtime.Modules), SourcePurgeStageHostNames.WriterAvailable));
            Require(!await settlement.WaitAsync(SourcePurgeStageHostNames.Deadline) && pending.IsCompleted);
            using var source = SourcePurgeStageHostBridge.SourceScope(runtime.Modules);
        }
        finally
        {
            writer?.Dispose();
            if (settlement is not null)
                await settlement;
            else
                SourcePurgeStageHostBridge.Finish(runtime.Modules, session);
        }
    }

    private static async Task PendingShutdownReentry()
    {
        await using var runtime = new ProcessRuntime();
        var announced = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? foreign = null;
        try
        {
            runtime.Modules.Apply(registry =>
            {
                foreign = Task.Run(async () =>
                {
                    Task disposal = runtime.DisposeAsync().AsTask();
                    announced.TrySetResult(disposal);
                    await disposal;
                });
                Task pending = announced.Task.WaitAsync(SourcePurgeStageHostNames.Deadline).GetAwaiter().GetResult();
                Require(!pending.IsCompleted);
                Refused(() => runtime.DisposeAsync().AsTask().GetAwaiter().GetResult(), SourcePurgeStageHostNames.ReentrantShutdown);
                Require(!pending.IsCompleted);
                registry.Apply(inner => inner.Register(
                    SourcePurgeStageHostNames.Clean,
                    SourcePurgeStageHostNames.Value,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                ));
            });
        }
        finally
        {
            if (foreign is not null)
                await foreign;
        }
        await runtime.DisposeAsync().AsTask().WaitAsync(SourcePurgeStageHostNames.Deadline);
    }

    private static async Task UnrelatedCancellation()
    {
        await using var runtime = new ProcessRuntime();
        ProcessHandle ordinary = runtime.Spawn(_ => throw new OperationCanceledException());
        Require((await ordinary.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(
            Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(nameof(OperationCanceledException)))
        ));
        await using var closed = new ProcessRuntime();
        await closed.DisposeAsync();
        ProcessHandle foreign = runtime.Spawn(async context =>
        {
            await SourcePurgeStageHostBridge.Publish(closed.Modules, context, () => Definition().Register(closed.Modules));

            return Nil.Value;
        });
        Require((await foreign.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(
            Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(SourcePurgeStageHostNames.ClosingException))
        ));
    }

    private static async Task UncaughtShutdown(bool prepare)
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        var announced = new TaskCompletionSource<(ProcessContext Context, Task Waiting)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodyFinally = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                Task waiting = prepare
                    ? SourcePurgeStageHostBridge.PrepareAsync(runtime.Modules, context)
                    : SourcePurgeStageHostBridge.Publish(runtime.Modules, context, () => Definition().Register(runtime.Modules)).AsTask();
                announced.TrySetResult((context, waiting));
                await waiting;
                throw new InvalidOperationException(SourcePurgeStageHostNames.Broken);
            }
            finally
            {
                bodyFinally.TrySetResult();
            }
        });
        var waitingActor = await announced.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
        Require(!waitingActor.Waiting.IsCompleted);
        object runtimeGate = typeof(ProcessRuntime).GetField(SourcePurgeStageHostNames.RuntimeGate, SourcePurgeStageHostNames.Members)!.GetValue(runtime)!;
        object permission = SourcePurgeStageHostBridge.SourcePermission(runtime.Modules);
        var stageClosing = (CancellationTokenSource)permission.GetType().GetField(SourcePurgeStageHostNames.StageClosing, SourcePurgeStageHostNames.Members)!.GetValue(permission)!;
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closer = new Thread(() =>
        {
            try
            {
                runtime.DisposeAsync().AsTask().GetAwaiter().GetResult();
                disposed.TrySetResult();
            }
            catch (Exception failure)
            {
                disposed.TrySetException(failure);
            }
        })
        {
            IsBackground = true
        };
        bool started = false;
        try
        {
            lock (runtimeGate)
            {
                closer.Start();
                started = true;
                bodyFinally.Task.WaitAsync(SourcePurgeStageHostNames.Deadline).GetAwaiter().GetResult();
                Require(stageClosing.IsCancellationRequested && !waitingActor.Context.Cancellation.IsCancellationRequested);
                Require(waitingActor.Waiting.IsFaulted || waitingActor.Waiting.IsCanceled);
                Require(!actor.Completion.IsCompleted && !disposed.Task.IsCompleted);
            }
        }
        finally
        {
            if (started)
            {
                await disposed.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
                Require(closer.Join(SourcePurgeStageHostNames.Deadline));
            }
        }
        Require((await actor.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline)).Equals(Term.A(ProcessExitReasons.Shutdown)));
        Require(runtime.Modules.PendingPurge(SourcePurgeStageHostBridge.Image(session)) is null);
    }
}
