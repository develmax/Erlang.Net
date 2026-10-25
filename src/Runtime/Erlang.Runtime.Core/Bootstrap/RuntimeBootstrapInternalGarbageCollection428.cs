namespace Erlang;

public static class RuntimeBootstrapInternalGarbageCollection428
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(InternalGarbageCollectionNames428.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallInternalGarbageCollection428(runtime);
    }
}
