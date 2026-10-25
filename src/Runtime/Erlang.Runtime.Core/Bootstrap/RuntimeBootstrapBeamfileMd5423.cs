namespace Erlang;

public static class RuntimeBootstrapBeamfileMd5423
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(BeamfileMd5Names423.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBeamfileMd5423(runtime);
    }
}
