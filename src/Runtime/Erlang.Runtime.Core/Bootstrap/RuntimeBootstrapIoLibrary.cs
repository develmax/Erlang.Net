namespace Erlang;

/// <summary>Installs the shared printable-range primitive before explicit io registration.</summary>
public static class RuntimeBootstrapIoLibrary
{
    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
        {
            throw new ArgumentException(LibrarySelectionNames.Owner, nameof(registry));
        }
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallManagedIoPrimitive();
    }
}
