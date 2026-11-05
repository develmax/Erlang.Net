namespace Erlang;

public static class RuntimeBootstrapEtsInfo420
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsInfoNames420.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        // The exact allocator admission belongs to memory measurement. Installing
        // the other ETS information operations must not require that provider.
        registry.InstallBootstrapEtsInfo420(runtime);
    }
}
