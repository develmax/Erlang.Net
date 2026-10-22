namespace Erlang;

public static class RuntimeBootstrapAllocatorGather434
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(AllocatorGatherNames434.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallAllocatorGather434(runtime);
    }
}
