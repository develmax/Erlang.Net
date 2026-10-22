namespace Erlang;

public static class RuntimeBootstrapDistDecode423
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(DistDecodeNames423.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDistDecode423(runtime);
    }
}
