namespace Erlang;

public static class RuntimeBootstrapMapKeys426
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(MapKeysNames426.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallMapKeys426(runtime);
    }
}
