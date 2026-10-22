namespace Erlang;

public static class RuntimeBootstrapEtsFixation420
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsFixationNames420.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapEtsFixation420(runtime);
    }
}
