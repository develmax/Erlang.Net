namespace Erlang;

public static class RuntimeBootstrapEtsCompiled419
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsCompiledNames419.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapEtsCompiled419(runtime);
    }
}
