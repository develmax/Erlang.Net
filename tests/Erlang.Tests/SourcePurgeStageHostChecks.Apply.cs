namespace Erlang.Tests;

public static partial class SourcePurgeStageHostChecks
{
    private static async Task ApplyAdoptionAndRollback()
    {
        await using var runtime = new ProcessRuntime();
        Definition(SourcePurgeStageHostNames.SeedSource).Register(runtime.Modules);
        var announced = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle? publisher = null;
        var marker = new InvalidOperationException(SourcePurgeStageHostNames.Rollback);
        bool rolledBack = false;
        Task transaction = Task.Run(() => runtime.Modules.Apply(registry =>
            {
                registry.Apply(nested => Definition().Register(nested));
                Definition(SourcePurgeStageHostNames.OtherSource).Register(registry);
                Require(registry.CurrentCodeImage(SourcePurgeStageHostNames.Module) is not null);
                publisher = runtime.Spawn(async context =>
                {
                    try
                    {
                        Task<bool> waiting = SourcePurgeStageHostBridge.Publish(
                            registry,
                            context,
                            () =>
                        {
                            Require(registry.CurrentCodeImage(SourcePurgeStageHostNames.Module) is null);
                            Require(registry.CurrentCodeImage(SourcePurgeStageHostNames.Other) is null);
                            Definition(SourcePurgeStageHostNames.ThirdSource).Register(registry);
                        }
                        ).AsTask();
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
                Task<bool> pending = announced.Task.WaitAsync(SourcePurgeStageHostNames.Deadline).GetAwaiter().GetResult();
                Require(!pending.IsCompleted);
                Task.Run(() => Refused(
                    () => Definition(SourcePurgeStageHostNames.ThirdSource).Register(registry),
                    SourcePurgeStageHostNames.StageBusy
                )).WaitAsync(SourcePurgeStageHostNames.Deadline).GetAwaiter().GetResult();
                throw marker;
            }));
        try
        {
            await transaction.WaitAsync(SourcePurgeStageHostNames.Deadline);
        }
        catch (Exception failure) when (ReferenceEquals(failure, marker))
        {
            rolledBack = true;
        }
        finally
        {
            try
            {
                await transaction;
            }
            catch (Exception failure) when (ReferenceEquals(failure, marker)) { }
        }
        Require(rolledBack);
        await published.Task.WaitAsync(SourcePurgeStageHostNames.Deadline);
        await publisher!.Completion.WaitAsync(SourcePurgeStageHostNames.Deadline);
        Require(runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Module) is null);
        Require(runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Other) is null);
        Require(runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Third) is not null);
        bool nullRefused = false;
        try
        {
            runtime.Modules.Apply(null!);
        }
        catch (ArgumentNullException)
        {
            nullRefused = true;
        }
        Require(nullRefused);
        Definition().Register(runtime.Modules);
    }

    private static async Task BusyApplyAndTickets()
    {
        await using var runtime = new ProcessRuntime();
        Definition().Register(runtime.Modules);
        runtime.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object session = SourcePurgeStageHostBridge.Prepare(runtime.Modules);
        int callbacks = 0;
        try
        {
            Refused(
                () => runtime.Modules.Apply(registry =>
            {
                callbacks++;
                registry.Register(
                    SourcePurgeStageHostNames.Clean,
                    SourcePurgeStageHostNames.Value,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
                Definition(SourcePurgeStageHostNames.OtherSource).Register(registry);
            }),
                SourcePurgeStageHostNames.StageBusy
            );
            Require(callbacks == 1 && !runtime.Modules.HasExport(SourcePurgeStageHostNames.Clean, SourcePurgeStageHostNames.Value, 0));
            Require(runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Other) is null);
        }
        finally
        {
            SourcePurgeStageHostBridge.Finish(runtime.Modules, session);
        }
        IDisposable disposed = SourcePurgeStageHostBridge.SourceTicket(runtime.Modules);
        disposed.Dispose();
        Refused(() => SourcePurgeStageHostBridge.ScopeFor(disposed), SourcePurgeStageHostNames.InvalidTicket);
        await Task.Run(() =>
        {
            using var current = SourcePurgeStageHostBridge.SourceScope(runtime.Modules);
            Task.Run(() => Refused(
                () => Definition(SourcePurgeStageHostNames.OtherSource).Register(runtime.Modules),
                SourcePurgeStageHostNames.StageBusy
            )).WaitAsync(SourcePurgeStageHostNames.Deadline).GetAwaiter().GetResult();
        }).WaitAsync(SourcePurgeStageHostNames.Deadline);
        Definition(SourcePurgeStageHostNames.OtherSource).Register(runtime.Modules);
        await using var foreign = new ProcessRuntime();
        Definition().Register(foreign.Modules);
        foreign.Modules.DeleteModule(SourcePurgeStageHostNames.Module);
        object foreignSession = SourcePurgeStageHostBridge.Prepare(foreign.Modules);
        try
        {
            await Task.Run(() =>
            {
                using var current = SourcePurgeStageHostBridge.SourceScope(runtime.Modules);
                Refused(() => Definition(SourcePurgeStageHostNames.OtherSource).Register(foreign.Modules), SourcePurgeStageHostNames.StageBusy);
            }).WaitAsync(SourcePurgeStageHostNames.Deadline);
        }
        finally
        {
            SourcePurgeStageHostBridge.Finish(foreign.Modules, foreignSession);
        }
    }

    private static async Task UnrelatedRetainedWriter()
    {
        await using var runtime = new ProcessRuntime();
        using (IDisposable ticket = await SourcePurgeStageHostBridge.CodeTicket(runtime.Modules))
        {
            Require(SourcePurgeStageHostBridge.WithCodeTicket(
                ticket,
                () =>
            {
                Refused(
                    () => runtime.Modules.Apply(registry =>
                {
                    registry.Register(
                        SourcePurgeStageHostNames.Clean,
                        SourcePurgeStageHostNames.Value,
                        0,
                        (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                    );
                    Definition().Register(registry);
                }),
                    SourcePurgeStageHostNames.WriterOrder
                );
                Require(runtime.Modules.CurrentCodeImage(SourcePurgeStageHostNames.Module) is null);
                Require(!runtime.Modules.HasExport(SourcePurgeStageHostNames.Clean, SourcePurgeStageHostNames.Value, 0));

                return true;
            }
            ));
        }
        Definition().Register(runtime.Modules);
    }
}
