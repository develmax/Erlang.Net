namespace Erlang;

public static class RuntimeBootstrapDistChannel429
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(DistChannel429Names.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDistChannel429(runtime);
    }
}
