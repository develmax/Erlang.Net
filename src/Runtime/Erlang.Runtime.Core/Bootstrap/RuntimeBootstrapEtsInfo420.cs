namespace Erlang;

public static class RuntimeBootstrapEtsInfo420
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsInfoNames420.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        _ = RuntimeManagedObjectExtent420.Measure(new object());
        registry.InstallBootstrapEtsInfo420(runtime);
    }
}
