namespace Erlang;

public static class RuntimeBootstrapDebugCode427
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(DebugCodeNames427.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDebugCode427(runtime);
    }
}
