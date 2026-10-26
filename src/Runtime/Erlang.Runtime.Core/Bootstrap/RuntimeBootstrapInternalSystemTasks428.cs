namespace Erlang;

public static class RuntimeBootstrapInternalSystemTasks428
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry)) throw new ArgumentException(SystemTaskNames428.Foreign, nameof(registry));
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallInternalSystemTasks428(runtime);
    }

    public static void StartOriginalHandlers(ProcessRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (runtime.Modules.SourceOwner(SystemTaskNames428.HandlerModule, SystemTaskNames428.Start, 0) is not PreparedBeamImage image)
            throw new InvalidOperationException(SystemTaskNames428.Startup);
        runtime.StartDirtySystemHandlers428(image);
    }
}
