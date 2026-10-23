namespace Erlang;

public static class RuntimeBootstrapEtsInspection420
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsInspectionNames420.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapEtsInspection420(runtime);
    }
}
