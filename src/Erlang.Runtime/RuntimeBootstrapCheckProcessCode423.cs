namespace Erlang;

public static class RuntimeBootstrapCheckProcessCode423
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(CheckProcessCodeNames423.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallCheckProcessCode423(runtime);
    }
}
