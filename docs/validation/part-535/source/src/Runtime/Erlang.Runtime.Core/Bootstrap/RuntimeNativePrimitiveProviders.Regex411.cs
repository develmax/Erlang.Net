namespace Erlang;

public static partial class RuntimeNativePrimitiveProviders
{
    public static async ValueTask<RuntimeRegexEngine411> RegisterRegexAsync411(
        ProcessRuntime runtime,
        ModuleRegistry registry,
        string officialAdmission,
        CancellationToken cancellation = default
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(ReNames411.Foreign);
        registry.RequireRegexRegistrationCaller411();
        cancellation.ThrowIfCancellationRequested();
        RuntimeRegexEngine411? engine = runtime.BeginRegexInstall411();
        if (engine is not null)
        {
            registry.InstallRegexProvider411(runtime, engine);

            return engine;
        }
        try
        {
            engine = RuntimeRegexEngine411.Open(runtime, officialAdmission);
            cancellation.ThrowIfCancellationRequested();
            runtime.PublishRegex411(engine);
            registry.InstallRegexProvider411(runtime, engine);
            engine.MarkPublished599();
            runtime.CompleteRegexInstall411(null);

            return engine;
        }
        catch (Exception original)
        {
            Exception? cleanupFailure = null, completionFailure = null;
            try
            {
                if (engine is not null) await engine.DisposeFailedInstallation599Async().ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                cleanupFailure = cleanup;
            }
            try
            {
                if (cleanupFailure is not null && engine is not null)
                    runtime.RetainFailedRegexInstallation599(engine);
                else
                    runtime.CompleteRegexInstall411(engine);
            }
            catch (Exception cleanup)
            {
                completionFailure = cleanup;
            }
            // Preserve all phases after attempting cleanup and ownership handoff.
            if (cleanupFailure is not null && completionFailure is not null)
                throw new AggregateException(original, cleanupFailure, completionFailure);
            if (cleanupFailure is not null || completionFailure is not null)
                throw new AggregateException(original, cleanupFailure ?? completionFailure!);
            throw;
        }
    }
}
