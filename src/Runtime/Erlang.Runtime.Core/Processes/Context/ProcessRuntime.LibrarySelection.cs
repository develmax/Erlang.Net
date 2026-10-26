namespace Erlang;

public sealed partial class ProcessRuntime
{
    public RuntimeLibrarySelectionSnapshot LibrarySelection { get; private set; } = RuntimeLibrarySelectionSnapshot.Empty;

    public static Task<ProcessRuntime> CreateAsync(
        RuntimeLibrarySelection libraries,
        CancellationToken cancellation = default
    ) => CreateAsync(libraries, RuntimeNativePrimitives.None, cancellation);

    public static Task<ProcessRuntime> CreateAsync(
        RuntimeLibrarySelection libraries,
        RuntimeNativePrimitives nativePrimitives,
        CancellationToken cancellation
    ) => CreateWithStartupOptionsAsync(
        libraries,
        new RuntimeLibraryStartupOptions(nativePrimitives),
        cancellation,
        false
    );

    public static Task<ProcessRuntime> CreateWithOptionsAsync(
        RuntimeLibrarySelection libraries,
        RuntimeLibraryStartupOptions options,
        CancellationToken cancellation
    ) => CreateWithStartupOptionsAsync(
        libraries,
        options,
        cancellation,
        true
    );

    private static async Task<ProcessRuntime> CreateWithStartupOptionsAsync(
        RuntimeLibrarySelection libraries,
        RuntimeLibraryStartupOptions options,
        CancellationToken cancellation,
        bool explicitOnLoad
    )
    {
        ArgumentNullException.ThrowIfNull(libraries);
        ArgumentNullException.ThrowIfNull(options);
        cancellation.ThrowIfCancellationRequested();
        var observer = libraryStartupObserver.Value;
        var runtime = new ProcessRuntime();
        Task? setup = null;
        try
        {
            if (options.DriverFrontend is { } driverFrontend)
            {
                // The runtime is still exclusively factory-owned. Await actual
                // startup/rollback rather than abandoning it with WaitAsync.
                DriverFrontendConfiguration422 configuration = driverFrontend.Bind(runtime, cancellation);
                await RuntimeBootstrapDriverFrontend422.Register(
                    runtime,
                    runtime.Modules,
                    configuration,
                    cancellation
                ).ConfigureAwait(false);
            }
            RuntimeLibraryPreparedSelection? plan = null;
            setup = runtime.InitializeModulesAsync(registry =>
            {
                RuntimeNativePrimitiveProviders.Register(runtime, registry, options.NativePrimitives);
                if (options.BootstrapFileSystem)
                    RuntimeBootstrapFileSystem.Register(runtime, registry);
                observer?.Invoke(LibraryStartupObservationNames.SetupBeforePrepare, runtime, null);
                cancellation.ThrowIfCancellationRequested();
                plan = explicitOnLoad
                    ? registry.PrepareStartupLibrariesWithOnLoad(libraries, options.OnLoadOrder)
                    : registry.PrepareStartupLibraries(libraries);
                cancellation.ThrowIfCancellationRequested();
                registry.PublishStartupLibraries(plan);
            }).AsTask();
            await setup.WaitAsync(cancellation).ConfigureAwait(false);
            runtime.LibrarySelection = plan!.Images.Count == 0
                ? runtime.Modules.CaptureStartupLibraries(plan)
                : await runtime.FinishStartupLibraries(plan, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();

            return runtime;
        }
        catch (Exception error)
        {
            var cleanup = new List<Exception>();
            try
            {
                observer?.Invoke(LibraryStartupObservationNames.CleanupStarted, runtime, null);
            }
            catch (Exception observationFailure)
            {
                cleanup.Add(observationFailure);
            }
            try
            {
                Task disposal = runtime.DisposeAsync().AsTask();
                try
                {
                    if (runtime.disposed)
                        observer?.Invoke(LibraryStartupObservationNames.CleanupRetirementMarked, runtime, null);
                }
                catch (Exception observationFailure)
                {
                    cleanup.Add(observationFailure);
                }
                await disposal.ConfigureAwait(false);
            }
            catch (Exception failedCleanup)
            {
                cleanup.Add(failedCleanup);
            }
            if (runtime.disposed)
            {
                try
                {
                    await runtime.actorDispatcher.Completion.ConfigureAwait(false);
                }
                catch (Exception failedDrain)
                {
                    cleanup.Add(failedDrain);
                }
            }
            if (setup is not null)
            {
                try
                {
                    await setup.ConfigureAwait(false);
                }
                catch
                {
                    // Observe the private queued operation after owned shutdown.
                }
            }
            if (cleanup.Count != 0)
                throw new AggregateException(LibrarySelectionNames.Cleanup, new[] { error }.Concat(cleanup));

            throw;
        }
    }

    private async Task<RuntimeLibrarySelectionSnapshot> FinishStartupLibraries(
        RuntimeLibraryPreparedSelection plan,
        CancellationToken cancellation
    )
    {
        var observer = libraryStartupObserver.Value;
        var result = new TaskCompletionSource<RuntimeLibrarySelectionSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = Spawn(async context =>
        {
            entered.TrySetResult(context);
            try
            {
                foreach (string module in plan.OnLoadOrder)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    BeamOnLoadResult loaded = await Modules.LoadPreparedOnLoadAsync(this, plan.Images[module]);
                    if (!loaded.Published)
                        throw new InvalidOperationException(
                            LibrarySelectionNames.OnLoadFailed
                            + LibrarySelectionNames.ContextSeparator + module
                            + LibrarySelectionNames.ContextSeparator + loaded.DownReason,
                            loaded.Outcome.Failure
                        );
                }
                Task admission = codePurger.WaitAsync(context.Cancellation);
                bool acquired = false;
                try
                {
                    await context.Mailbox.WaitForControlCompletion(admission, context.Cancellation);
                    await admission;
                    acquired = true;
                    observer?.Invoke(LibraryStartupObservationNames.PurgerPermitAcquired, this, context);
                    foreach (string module in plan.Images.Keys.Order(StringComparer.Ordinal))
                        if (Modules.HasOldCode(module) && !await PurgeOldCode(context, module))
                            throw new InvalidOperationException(LibrarySelectionNames.Purge + LibrarySelectionNames.ContextSeparator + module);
                }
                finally
                {
                    if (!acquired)
                    {
                        try
                        {
                            await admission;
                            acquired = true;
                        }
                        catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
                        {
                        }
                    }
                    if (acquired)
                        codePurger.Release();
                }
                result.TrySetResult(Modules.CaptureStartupLibraries(plan));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
                _ = result.Task.Exception;
            }

            return Nil.Value;
        });
        Task first = await Task.WhenAny(result.Task, worker.Completion).WaitAsync(cancellation).ConfigureAwait(false);
        if (ReferenceEquals(first, worker.Completion) && !result.Task.IsCompleted)
            throw new InvalidOperationException(LibrarySelectionNames.Worker);
        Term exit = await worker.Completion.WaitAsync(cancellation).ConfigureAwait(false);
        if (entered.Task.IsCompletedSuccessfully)
            await entered.Task.Result.Unwound.Task.WaitAsync(cancellation).ConfigureAwait(false);
        if (!exit.Equals(Term.A(ProcessExitReasons.Normal)))
            throw new InvalidOperationException(LibrarySelectionNames.Worker + LibrarySelectionNames.ContextSeparator + exit);

        return await result.Task.WaitAsync(cancellation).ConfigureAwait(false);
    }
}
