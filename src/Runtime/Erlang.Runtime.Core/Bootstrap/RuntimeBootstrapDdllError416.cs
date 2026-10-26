namespace Erlang;

public static class RuntimeBootstrapDdllError416
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(DdllErrorNames416.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        if (!runtime.HasWindowsDdllError416) throw new PlatformNotSupportedException(DdllErrorNames416.Profile);
        registry.InstallDdllError416(runtime);
    }
}
