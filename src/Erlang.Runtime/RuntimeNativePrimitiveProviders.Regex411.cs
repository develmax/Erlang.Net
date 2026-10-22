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
            runtime.CompleteRegexInstall411(null);

            return engine;
        }
        catch
        {
            try
            {
                if (engine is not null) await engine.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                runtime.CompleteRegexInstall411(engine);
            }
            throw;
        }
    }
}
