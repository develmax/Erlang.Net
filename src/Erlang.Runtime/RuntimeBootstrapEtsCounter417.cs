namespace Erlang;

public static class RuntimeBootstrapEtsCounter417
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(EtsCounterNames417.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapEtsCounter417(runtime);
    }
}
