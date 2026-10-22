namespace Erlang;

public sealed class RuntimeLibrarySelectionSnapshot
{
    internal static RuntimeLibrarySelectionSnapshot Empty { get; } = new(Array.Empty<RuntimeLibraryModuleSnapshot>());

    internal RuntimeLibrarySelectionSnapshot(IEnumerable<RuntimeLibraryModuleSnapshot> modules)
    {
        Modules = Array.AsReadOnly(modules.OrderBy(value => value.Module, StringComparer.Ordinal).ToArray());
    }

    public IReadOnlyList<RuntimeLibraryModuleSnapshot> Modules { get; }
}
