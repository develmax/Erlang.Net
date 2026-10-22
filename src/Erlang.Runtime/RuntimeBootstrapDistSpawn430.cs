namespace Erlang;

public static class RuntimeBootstrapDistSpawn430
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(DistSpawn430Names.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDistSpawn430(runtime);
    }
}
