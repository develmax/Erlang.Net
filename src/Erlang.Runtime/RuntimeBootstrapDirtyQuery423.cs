namespace Erlang;

public static class RuntimeBootstrapDirtyQuery423
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(DirtyQueryNames423.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDirtyQuery423(runtime);
    }
}
