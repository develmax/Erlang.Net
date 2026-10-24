namespace Erlang.Tests;

public static class PreloadedModuleHostChecks
{
    public static async Task Run()
    {
        var registry = new ModuleRegistry();
        registry.PublishModule(PreloadedModuleHostNames.User);
        var before = registry.PreloadedModules;
        registry.InstallPreloaded(target =>
        {
            target.Register(
                PreloadedModuleHostNames.Embedded,
                PreloadedModuleHostNames.Function,
                0,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            target.PublishModule(PreloadedModuleHostNames.Empty);
        });
        var installed = registry.PreloadedModules;
        if (installed.Count != 2 || !installed.Contains(PreloadedModuleHostNames.Embedded) || !installed.Contains(PreloadedModuleHostNames.Empty) || installed.Contains(PreloadedModuleHostNames.User))
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Ownership);
        if (before.Count != 0)
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Snapshot);
        try
        {
            registry.Apply(target =>
            {
                target.InstallPreloaded(embedded => embedded.PublishModule(PreloadedModuleHostNames.Failed));
                throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Rollback);
            });
        }
        catch (InvalidOperationException exception) when (exception.Message == PreloadedModuleHostDiagnostics.Rollback)
        {
        }
        if (registry.IsModuleLoaded(PreloadedModuleHostNames.Failed) || registry.PreloadedModules.Count != 2)
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Rollback);
        bool rejected = false;
        try
        {
            registry.InstallPreloaded(target =>
            {
                target.PublishModule(PreloadedModuleHostNames.Failed);
                target.Register(
                    PreloadedModuleHostNames.Embedded,
                    PreloadedModuleHostNames.Function,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
            });
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        if (!rejected || registry.IsModuleLoaded(PreloadedModuleHostNames.Failed) || registry.PreloadedModules.Count != 2)
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Rollback);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var readerStarted = new ManualResetEventSlim();
        using var readerCompleted = new ManualResetEventSlim();
        var writer = Task.Run(() => registry.InstallPreloaded(target =>
        {
            target.PublishModule(PreloadedModuleHostNames.Concurrent);
            entered.Set();
            release.Wait();
            target.Register(
                PreloadedModuleHostNames.Concurrent,
                PreloadedModuleHostNames.Function,
                0,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
        }));
        if (!entered.Wait(PreloadedModuleHostParameters.Deadline))
        {
            release.Set();
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Concurrency);
        }
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new Thread(() =>
        {
            readerStarted.Set();
            try
            {
                result.SetResult(registry.PreloadedModules.Contains(PreloadedModuleHostNames.Concurrent) && registry.HasExport(PreloadedModuleHostNames.Concurrent, PreloadedModuleHostNames.Function, 0));
            }
            catch (Exception exception)
            {
                result.SetException(exception);
            }
            finally
            {
                readerCompleted.Set();
            }
        })
        { IsBackground = true };
        reader.Start();
        bool partial;
        try
        {
            partial = !readerStarted.Wait(PreloadedModuleHostParameters.Deadline) || readerCompleted.Wait(PreloadedModuleHostParameters.BlockedObservation);
        }
        finally
        {
            release.Set();
        }
        await writer.WaitAsync(PreloadedModuleHostParameters.Deadline);
        if (!reader.Join(PreloadedModuleHostParameters.Deadline) || partial || !await result.Task.WaitAsync(PreloadedModuleHostParameters.Deadline))
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Concurrency);
        if (installed.Count != 2)
            throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Snapshot);

        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var first = await runtime.Modules.Call(context, PreloadedModuleHostNames.Erlang, PreloadedModuleHostNames.PreLoaded);
                var second = await runtime.Modules.Call(context, PreloadedModuleHostNames.Erlang, PreloadedModuleHostNames.PreLoaded);
                var names = Cons.Items(first).Cast<Atom>().Select(atom => atom.Name).ToHashSet(StringComparer.Ordinal);
                if (!names.Contains(PreloadedModuleHostNames.Init) || !names.Contains(PreloadedModuleHostNames.Erlang) || !first.Equals(second) || ReferenceEquals(first, second) || first is not Cons firstCell || second is not Cons secondCell || !ReferenceEquals(firstCell.Head, secondCell.Head) || names.Any(name => !runtime.Modules.IsModuleLoaded(name)))
                    throw new InvalidOperationException(PreloadedModuleHostDiagnostics.Boot);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await process.Completion.WaitAsync(PreloadedModuleHostParameters.Deadline);
        if (failure is not null)
            throw failure;
    }
}
