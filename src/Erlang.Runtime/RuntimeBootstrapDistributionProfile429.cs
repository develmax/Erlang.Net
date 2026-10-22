namespace Erlang;

public static class RuntimeBootstrapDistributionProfile429
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(InternalDistributionProfile429Names.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDistributionProfile429(runtime);
    }
}
