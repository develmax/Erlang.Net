using System.Reflection;

namespace Erlang.Tests;

public static class BeamLoaderAdmissionRaceChecks
{
    public static async Task Run(byte[] bytes)
    {
        await CompetingConsume(bytes);
        await ClosedBeforeAdmission(bytes);
    }

    private static async Task CompetingConsume(byte[] bytes)
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreatePrepared(image);
        object moduleGate = typeof(ModuleRegistry).GetField(BeamLoaderAdmissionRaceNames.ModuleGate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            var announced = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool>? competing = null;
            await Task.Run(() =>
            {
                using var source = SourcePurgeStageHostBridge.SourceScope(runtime.Modules);
                using var modification = EnterModification(runtime.Modules);
                Monitor.Enter(moduleGate);
                try
                {
                    competing = Task.Run(async () =>
                    {
                        try
                        {
                            Task<bool> waiting = SourcePurgeStageHostBridge.Publish(
                                runtime.Modules, context, () => Finish(runtime.ReferenceResources, reference)
                            ).AsTask();
                            announced.TrySetResult(waiting);

                            return await waiting;
                        }
                        catch (Exception failure)
                        {
                            announced.TrySetException(failure);
                            throw;
                        }
                    });
                    Task<bool> waiting = announced.Task.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds)).GetAwaiter().GetResult();
                    Require(!waiting.IsCompleted);
                    Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                }
                finally
                {
                    Monitor.Exit(moduleGate);
                }
            });
            await Badarg(competing!);
            Require(image.IsConsumed);
        }
        );
    }

    private static async Task ClosedBeforeAdmission(byte[] bytes)
    {
        var runtime = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        ResourceReferenceTerm reference = runtime.ReferenceResources.CreatePrepared(image);
        using var started = new ManualResetEventSlim();
        object moduleGate = typeof(ModuleRegistry).GetField(BeamLoaderAdmissionRaceNames.ModuleGate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
        Task<Term> competing;
        using (EnterModification(runtime.Modules))
        {
            Monitor.Enter(moduleGate);
            try
            {
                competing = Task.Run(() =>
                {
                    started.Set();

                    return Finish(runtime.ReferenceResources, reference);
                });
                Require(started.Wait(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds)));
                WaitForWriter(runtime.Modules);
                runtime.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                Monitor.Exit(moduleGate);
            }
        }
        await Badarg(competing);
        Require(!image.IsConsumed && runtime.ReferenceResources.LiveResources == 0);
    }

    private static object Permission(ModuleRegistry registry) => typeof(ModuleRegistry).GetProperty(
        BeamLoaderAdmissionRaceNames.CodeModification,
        BindingFlags.Instance | BindingFlags.NonPublic
    )!.GetValue(registry)!;

    private static IDisposable EnterModification(ModuleRegistry registry) => (IDisposable)Permission(registry).GetType().GetMethod(
        BeamLoaderAdmissionRaceNames.Enter,
        BindingFlags.Instance | BindingFlags.NonPublic
    )!.Invoke(Permission(registry), null)!;

    private static void WaitForWriter(ModuleRegistry registry)
    {
        object permission = Permission(registry);
        PropertyInfo pending = permission.GetType().GetProperty(
            BeamLoaderAdmissionRaceNames.PendingWriters,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while ((int)pending.GetValue(permission)! == 0)
        {
            Require(watch.Elapsed < TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
            Thread.Yield();
        }
    }

    private static Term Finish(RuntimeReferenceResources resources, ReferenceTerm reference)
    {
        MethodInfo method = typeof(RuntimeReferenceResources).GetMethod(BeamLoaderAdmissionRaceNames.FinishReferences, BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            return (Term)method.Invoke(resources, [new ReferenceTerm[] { reference }])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is ErlangException error)
        {
            throw error;
        }
    }

    private static async Task Badarg(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));

            return;
        }

        throw new InvalidOperationException(BeamLoaderAdmissionRaceNames.Failure);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamLoaderAdmissionRaceNames.Failure);
    }
}
