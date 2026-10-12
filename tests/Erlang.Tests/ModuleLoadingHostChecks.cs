using Erlang.Compiler;

namespace Erlang.Tests;

public static class ModuleLoadingHostChecks
{
    public static async Task Run()
    {
        var registry = new ModuleRegistry();
        registry.Register(
            ModuleLoadingHostNames.Original,
            ModuleLoadingHostNames.Function,
            0,
            (_, _) => ValueTask.FromResult<Term>(Term.I(1))
        );
        var before = registry.LoadedModules;
        new Parser("-module(module_loading_empty).").ParseModule().Register(registry);
        if (!registry.IsModuleLoaded(ModuleLoadingHostNames.Empty) || !registry.IsModuleLoaded(ModuleLoadingHostNames.Original))
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.MissingCurrent);
        if (before.Count != 1 || before.Contains(ModuleLoadingHostNames.Empty))
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.SnapshotChanged);

        bool rejected = false;
        try
        {
            new ModuleDefinition(ModuleLoadingHostNames.Failed, [(ModuleLoadingHostNames.Function, 0), (ModuleLoadingHostNames.Function, 0)], []).Register(registry);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        if (!rejected)
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.DuplicateAccepted);
        if (registry.IsModuleLoaded(ModuleLoadingHostNames.Failed) || registry.HasExport(ModuleLoadingHostNames.Failed, ModuleLoadingHostNames.Function, 0) || !registry.IsModuleLoaded(ModuleLoadingHostNames.Original))
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.FailedVisible);

        try
        {
            registry.Apply(target =>
            {
                target.PublishModule(ModuleLoadingHostNames.Unpublished);
                throw new InvalidOperationException(ModuleLoadingHostDiagnostics.FailedVisible);
            });
        }
        catch (InvalidOperationException exception) when (exception.Message == ModuleLoadingHostDiagnostics.FailedVisible)
        {
        }
        if (registry.IsModuleLoaded(ModuleLoadingHostNames.Unpublished))
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.FailedVisible);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var readerStarted = new ManualResetEventSlim();
        using var readerCompleted = new ManualResetEventSlim();
        var writer = Task.Run(() => registry.Apply(target =>
        {
            target.Register(
                ModuleLoadingHostNames.Concurrent,
                ModuleLoadingHostNames.Function,
                0,
                (_, _) => ValueTask.FromResult<Term>(Term.I(2))
            );
            entered.Set();
            release.Wait();
            target.Register(
                ModuleLoadingHostNames.Concurrent,
                ModuleLoadingHostNames.Function,
                1,
                (_, arguments) => ValueTask.FromResult(arguments[0])
            );
        }));
        if (!entered.Wait(ModuleLoadingHostParameters.WorkerDeadline))
        {
            release.Set();
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.PartialVisible);
        }
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new Thread(() =>
        {
            readerStarted.Set();
            try
            {
                result.SetResult(registry.IsModuleLoaded(ModuleLoadingHostNames.Concurrent) && registry.HasExport(ModuleLoadingHostNames.Concurrent, ModuleLoadingHostNames.Function, 1));
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
            partial = !readerStarted.Wait(ModuleLoadingHostParameters.WorkerDeadline) || readerCompleted.Wait(ModuleLoadingHostParameters.BlockedReaderObservation);
        }
        finally
        {
            release.Set();
        }
        await writer.WaitAsync(ModuleLoadingHostParameters.WorkerDeadline);
        if (partial || !await result.Task.WaitAsync(ModuleLoadingHostParameters.WorkerDeadline))
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.PartialVisible);
        var snapshot = registry.LoadedModules;
        if (snapshot.Count != snapshot.Distinct(StringComparer.Ordinal).Count())
            throw new InvalidOperationException(ModuleLoadingHostDiagnostics.SnapshotChanged);
    }
}
