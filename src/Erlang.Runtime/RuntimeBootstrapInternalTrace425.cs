namespace Erlang;

public static class RuntimeBootstrapInternalTrace425
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(InternalTraceNames425.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallInternalTrace425(runtime);
    }
}
