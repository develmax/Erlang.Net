namespace Erlang;

public static class RuntimeBootstrapProcessAlive425
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(ProcessAliveNames425.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallProcessAlive425(runtime);
    }
}
