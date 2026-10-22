namespace Erlang;

public static class RuntimeBootstrapDebugSharing425
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(DebugSharingNames425.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDebugSharing425(runtime);
    }
}
