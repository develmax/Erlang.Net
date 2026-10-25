namespace Erlang;

public static class RuntimeBootstrapDebugDirty424
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(DebugDirtyNames424.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDebugDirty424(runtime);
    }
}
