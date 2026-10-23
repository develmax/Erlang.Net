namespace Erlang;

public sealed class RuntimeModuleSelection
{
    private readonly byte[] image;

    private RuntimeModuleSelection(
        string module,
        RuntimeModuleBackend backend,
        ReadOnlyMemory<byte> bytes,
        IEnumerable<string> dependencies
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentNullException.ThrowIfNull(dependencies);
        if (backend == RuntimeModuleBackend.Managed && !LibrarySelectionNames.ManagedModules.Contains(module, StringComparer.Ordinal))
            throw new ArgumentException(LibrarySelectionNames.BeamOnly, nameof(module));
        string[] requested = dependencies.ToArray();
        foreach (string dependency in requested)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dependency);
        }
        if (requested.Distinct(StringComparer.Ordinal).Count() != requested.Length)
            throw new ArgumentException(LibrarySelectionNames.Request, nameof(dependencies));
        if (backend == RuntimeModuleBackend.Beam && bytes.IsEmpty)
            throw new ArgumentException(LibrarySelectionNames.Request, nameof(bytes));
        Module = module;
        Backend = backend;
        image = bytes.ToArray();
        Dependencies = Array.AsReadOnly(requested.Where(value => value != module).Order(StringComparer.Ordinal).ToArray());
    }

    public string Module { get; }
    public RuntimeModuleBackend Backend { get; }
    public IReadOnlyList<string> Dependencies { get; }
    internal ReadOnlyMemory<byte> OwnedImage => image;

    public static RuntimeModuleSelection Managed(string module)
        => new(
            module,
            RuntimeModuleBackend.Managed,
            ReadOnlyMemory<byte>.Empty,
            Array.Empty<string>()
        );

    public static RuntimeModuleSelection Beam(
        string module,
        ReadOnlyMemory<byte> image,
        IEnumerable<string>? dependencies = null
    ) => new(
        module,
        RuntimeModuleBackend.Beam,
        image,
        dependencies ?? Array.Empty<string>()
    );
}
