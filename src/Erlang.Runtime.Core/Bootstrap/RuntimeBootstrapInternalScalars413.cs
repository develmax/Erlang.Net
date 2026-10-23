namespace Erlang;

public static class RuntimeBootstrapInternalScalars413
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(ErtsInternalScalarNativeNames413.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallInternalScalars413(runtime);
    }
}
