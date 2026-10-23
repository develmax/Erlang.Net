namespace Erlang;

public static class RuntimeBootstrapInternalPorts416
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(InternalPortNames416.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallInternalPorts416(runtime);
    }
}
