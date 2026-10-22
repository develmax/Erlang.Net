namespace Erlang;

public sealed class RuntimeLibraryStartupOptions
{
    public RuntimeLibraryStartupOptions(
        RuntimeNativePrimitives nativePrimitives = RuntimeNativePrimitives.None,
        bool bootstrapFileSystem = false,
        IEnumerable<string>? onLoadOrder = null
    )
    {
        RuntimeNativePrimitiveProviders.Validate(nativePrimitives);
        string[] order = onLoadOrder?.ToArray() ?? Array.Empty<string>();
        foreach (string module in order)
            ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (order.Distinct(StringComparer.Ordinal).Count() != order.Length)
            throw new ArgumentException(LibrarySelectionNames.OnLoadOrder, nameof(onLoadOrder));
        NativePrimitives = nativePrimitives;
        BootstrapFileSystem = bootstrapFileSystem;
        OnLoadOrder = Array.AsReadOnly(order);
    }

    public RuntimeLibraryStartupOptions(
        RuntimeNativePrimitives nativePrimitives,
        bool bootstrapFileSystem,
        IEnumerable<string>? onLoadOrder,
        RuntimeDriverFrontendOptions driverFrontend
    ) : this(nativePrimitives, bootstrapFileSystem, onLoadOrder)
    {
        ArgumentNullException.ThrowIfNull(driverFrontend);
        DriverFrontend = driverFrontend;
    }

    public RuntimeDriverFrontendOptions? DriverFrontend { get; }

    public RuntimeNativePrimitives NativePrimitives { get; }
    public bool BootstrapFileSystem { get; }
    public IReadOnlyList<string> OnLoadOrder { get; }
}
