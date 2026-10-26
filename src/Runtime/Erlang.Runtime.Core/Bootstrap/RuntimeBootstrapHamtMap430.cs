namespace Erlang;

public static class RuntimeBootstrapHamtMap430
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(HamtNativeNames430.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallHamtMap430(runtime);
    }
}
