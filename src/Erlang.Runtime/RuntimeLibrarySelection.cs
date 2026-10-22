namespace Erlang;

public sealed class RuntimeLibrarySelection
{
    public RuntimeLibrarySelection(IEnumerable<RuntimeModuleSelection> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        RuntimeModuleSelection[] owned = modules.ToArray();
        foreach (RuntimeModuleSelection module in owned)
            ArgumentNullException.ThrowIfNull(module);
        if (owned.Select(value => value.Module).Distinct(StringComparer.Ordinal).Count() != owned.Length)
            throw new ArgumentException(LibrarySelectionNames.Request, nameof(modules));
        var selected = owned.Select(value => value.Module).ToHashSet(StringComparer.Ordinal);
        foreach (RuntimeModuleSelection module in owned)
            foreach (string dependency in module.Dependencies)
                if (!selected.Contains(dependency))
                    throw new ArgumentException(LibrarySelectionNames.Dependency, nameof(modules));
        Modules = Array.AsReadOnly(owned.OrderBy(value => value.Module, StringComparer.Ordinal).ToArray());
    }

    public IReadOnlyList<RuntimeModuleSelection> Modules { get; }
}
