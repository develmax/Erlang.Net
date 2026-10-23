namespace Erlang;

public static class RuntimeBootstrapDebugDispatch432
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(DebugDispatchNames432.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallDebugDispatch432(runtime);
    }
}
