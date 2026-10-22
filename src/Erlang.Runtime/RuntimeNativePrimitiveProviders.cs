namespace Erlang;

public static partial class RuntimeNativePrimitiveProviders
{
    public static void Register(
        ProcessRuntime runtime,
        ModuleRegistry registry,
        RuntimeNativePrimitives selection
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        Validate(selection);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(RuntimeNativePrimitiveNames.ForeignRegistry, nameof(registry));
        if (selection != RuntimeNativePrimitives.None)
            runtime.RequireNativePrimitiveRegistration();
        registry.InstallNativePrimitives(runtime, selection);
    }

    internal static void Validate(RuntimeNativePrimitives selection)
    {
        if ((selection & ~(RuntimeNativePrimitives.PersistentTermCore | RuntimeNativePrimitives.EtsStorageCore)) != 0)
            throw new ArgumentOutOfRangeException(nameof(selection));
    }
}
