using System.Reflection;

namespace Erlang.Tests;

public static class BeamLoaderAdmissionRaceChecks
{
    public static async Task Run(byte[] bytes)
    {
        await CompetingConsume(bytes);
        await ClosedBeforeAdmission(bytes);
        await ActiveHelperRefusesRetainedMonitor(true);
        await ActiveHelperRefusesRetainedMonitor(false);
        await PreparingGcRefusesRetainedMonitor();
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


    private static object? ReadHelperField(object value, string name)
        => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(value);

    private static object? ReadHelperProperty(object value, string name)
        => value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(value);

    private static object?[] HelperShutdownState(ProcessRuntime runtime)
        => [ReadHelperField(runtime, BeamLoaderAdmissionRaceNames.RuntimeDisposed),
            ReadHelperField(runtime, BeamLoaderAdmissionRaceNames.RuntimeHalt),
            ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.SourceClosing),
            ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.NifClosing),
            ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.SourceShutdown),
            ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.NifShutdown),
            ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcClosing),
            ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.TrapClosing)];

    private static async Task ActiveHelperRefusesRetainedMonitor(bool garbageCollection)
    {
        var failures = new List<Exception>(4);
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? admission = null;
        object? image = null;
        Task? literalRetirement = null;
        try
        {
            actor = new RuntimeNodeFoundationHostActor(runtime);
            object actual = await actor.InvokeAsync(async context =>
            {
                string name = garbageCollection ? BeamLoaderAdmissionRaceNames.AdmitGc : BeamLoaderAdmissionRaceNames.AdmitTrap;
                object[] arguments = garbageCollection ? [context]
                    : [context, BeamLoaderAdmissionRaceNames.TrapFunction, 2];
                object pending = typeof(ModuleRegistry).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(runtime.Modules, arguments)!;
                Task task = (Task)pending.GetType().GetMethod(BeamLoaderAdmissionRaceNames.AsTask)!.Invoke(pending, null)!;
                await task;

                return task.GetType().GetProperty(BeamLoaderAdmissionRaceNames.Result)!.GetValue(task)!;
            });
            admission = (IDisposable)actual;
            image = ReadHelperProperty(actual, BeamLoaderAdmissionRaceNames.HelperImage)!;
            Require(ReferenceEquals(
                image,
                ReadHelperField(
                    runtime.Modules,
                    garbageCollection ? BeamLoaderAdmissionRaceNames.GcPrivateImage : BeamLoaderAdmissionRaceNames.TrapPrivateImage
                )
            ));
            object domain = ReadHelperProperty(image, BeamLoaderAdmissionRaceNames.LiteralDomain)!;
            literalRetirement = (Task)ReadHelperProperty(domain, BeamLoaderAdmissionRaceNames.LiteralRetirement)!;
            string uses = garbageCollection ? BeamLoaderAdmissionRaceNames.GcUses : BeamLoaderAdmissionRaceNames.TrapUses;
            object moduleGate = typeof(ModuleRegistry).GetField(BeamLoaderAdmissionRaceNames.ModuleGate,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
            lock (moduleGate)
            {
                Require((int)ReadHelperField(runtime.Modules, uses)! == 1 && !literalRetirement.IsCompleted);
                object?[] before = HelperShutdownState(runtime);
                Exception? refusal = null;
                try
                {
                    ValueTask attemptedClose = runtime.DisposeAsync();
                    // A wrong asynchronous close must fail this fixture, never
                    // block while this Host retains the real module monitor.
                    if (attemptedClose.IsCompleted) attemptedClose.GetAwaiter().GetResult();
                }
                catch (Exception error)
                {
                    refusal = error;
                }
                Require(refusal is not null
                    && refusal.GetType().FullName == BeamLoaderAdmissionRaceNames.AdmissionException
                    && ReadHelperProperty(refusal, BeamLoaderAdmissionRaceNames.AdmissionFailure)!.ToString()
                        == BeamLoaderAdmissionRaceNames.RegistryReentry);
                Require(before.SequenceEqual(HelperShutdownState(runtime))
                    && (int)ReadHelperField(runtime.Modules, uses)! == 1
                    && !(bool)ReadHelperProperty(image, BeamLoaderAdmissionRaceNames.ImageDisposed)!
                    && !literalRetirement.IsCompleted);
            }
            Require(await actor.InvokeAsync(context => Task.FromResult(!context.Cancellation.IsCancellationRequested)));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                admission?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (actor is not null)
                {
                    await actor.DisposeAsync();
                    Require((await actor.Process.Completion.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds)))
                        .Equals(Term.A(ProcessExitReasons.Normal)));
                }
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count != 0) throw new AggregateException(failures);
        Require(image is not null && (bool)ReadHelperProperty(image, BeamLoaderAdmissionRaceNames.ImageDisposed)!);
        await literalRetirement!.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
    }


    private static async Task PreparingGcRefusesRetainedMonitor()
    {
        var failures = new List<Exception>(6);
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? sourceTicket = null;
        IDisposable? admission = null;
        Task<object>? admitted = null;
        object? image = null;
        Task? literalRetirement = null;
        try
        {
            sourceTicket = SourcePurgeStageHostBridge.SourceTicket(runtime.Modules);
            Require(sourceTicket is not null);
            Require(ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcPrivateImage) is null);
            actor = new RuntimeNodeFoundationHostActor(runtime);
            var entered = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
            admitted = actor.InvokeAsync(async context =>
            {
                try
                {
                    object value = typeof(ModuleRegistry).GetMethod(BeamLoaderAdmissionRaceNames.AdmitGc,
                        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(runtime.Modules, [context])!;
                    Task pending = (Task)value.GetType().GetMethod(BeamLoaderAdmissionRaceNames.AsTask)!.Invoke(value, null)!;
                    Require(!pending.IsCompleted);
                    // The real GC producer has entered AcquireAsync and is
                    // blocked by our actual source ticket before this signal.
                    entered.TrySetResult(pending);
                    await pending;

                    return pending.GetType().GetProperty(BeamLoaderAdmissionRaceNames.Result)!.GetValue(pending)!;
                }
                catch (Exception error)
                {
                    entered.TrySetException(error);
                    throw;
                }
            });
            Task preparing = await entered.Task.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
            object moduleGate = typeof(ModuleRegistry).GetField(BeamLoaderAdmissionRaceNames.ModuleGate,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
            object helperGate = ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcStateGate)!;
            lock (moduleGate)
            {
                lock (helperGate)
                {
                    Require(!preparing.IsCompleted
                        && (bool)ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcPreparing)!
                        && (int)ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcUses)! == 0
                        && ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcPrivateImage) is null);
                    object?[] before = HelperShutdownState(runtime);
                    Exception? refusal = null;
                    try
                    {
                        ValueTask attemptedClose = runtime.DisposeAsync();
                        if (attemptedClose.IsCompleted) attemptedClose.GetAwaiter().GetResult();
                    }
                    catch (Exception error)
                    {
                        refusal = error;
                    }
                    Require(refusal is not null
                        && refusal.GetType().FullName == BeamLoaderAdmissionRaceNames.AdmissionException
                        && ReadHelperProperty(refusal, BeamLoaderAdmissionRaceNames.AdmissionFailure)!.ToString()
                            == BeamLoaderAdmissionRaceNames.RegistryReentry);
                    Require(before.SequenceEqual(HelperShutdownState(runtime))
                        && (bool)ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcPreparing)!
                        && (int)ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcUses)! == 0
                        && !preparing.IsCompleted);
                }
            }
            // Release actual preparation custody BEFORE joining admission.
            sourceTicket!.Dispose();
            sourceTicket = null;
            object actual = await admitted.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
            admission = (IDisposable)actual;
            image = ReadHelperProperty(actual, BeamLoaderAdmissionRaceNames.HelperImage)!;
            Require(ReferenceEquals(image, ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcPrivateImage))
                && (int)ReadHelperField(runtime.Modules, BeamLoaderAdmissionRaceNames.GcUses)! == 1);
            object domain = ReadHelperProperty(image, BeamLoaderAdmissionRaceNames.LiteralDomain)!;
            literalRetirement = (Task)ReadHelperProperty(domain, BeamLoaderAdmissionRaceNames.LiteralRetirement)!;
            Require(!literalRetirement.IsCompleted);
            Require(await actor.InvokeAsync(context => Task.FromResult(!context.Cancellation.IsCancellationRequested)));
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                sourceTicket?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            // Even a failed assertion must join the real preparation after its
            // ticket is released, then release any admitted source lease.
            if (admission is null && admitted is not null)
            {
                try
                {
                    admission = (IDisposable)await admitted.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
            try
            {
                admission?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (actor is not null)
                {
                    await actor.DisposeAsync();
                    Require((await actor.Process.Completion.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds)))
                        .Equals(Term.A(ProcessExitReasons.Normal)));
                }
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count != 0) throw new AggregateException(failures);
        Require(image is not null && (bool)ReadHelperProperty(image, BeamLoaderAdmissionRaceNames.ImageDisposed)!);
        await literalRetirement!.WaitAsync(TimeSpan.FromSeconds(BeamLoaderAdmissionRaceNames.WatchdogSeconds));
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
