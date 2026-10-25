namespace Erlang;

public static class RuntimeBootstrapScheduler
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(RuntimeBootstrapSchedulerNames.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapScheduler(runtime);
    }

}
