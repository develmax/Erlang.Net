namespace Erlang;

public static class RuntimeBootstrapEtsMutations414
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsMutationNames414.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapEtsMutations414(runtime);
    }
}
