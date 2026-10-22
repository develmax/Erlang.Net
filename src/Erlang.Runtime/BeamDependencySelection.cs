using System.Collections.Frozen;

namespace Erlang;

public sealed class BeamDependencySelection
{
    public BeamDependencySelection(IEnumerable<string> registryModules)
    {
        ArgumentNullException.ThrowIfNull(registryModules);
        string[] modules = registryModules.ToArray();
        foreach (string module in modules)
            ArgumentException.ThrowIfNullOrWhiteSpace(module);
        RegistryModules = modules.ToFrozenSet(StringComparer.Ordinal);
    }

    public IReadOnlySet<string> RegistryModules { get; }
}
