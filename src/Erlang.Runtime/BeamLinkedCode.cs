using System.Collections.Frozen;
using System.Collections.Concurrent;

namespace Erlang;

internal sealed record BeamLinkedCode(
    BeamStructure Structure,
    BeamTypeTable Types,
    IReadOnlyList<BeamInstruction> Instructions,
    IReadOnlyList<BeamLinkedFunction> Functions,
    IReadOnlyDictionary<int, int> Labels,
    IReadOnlyList<BeamLinkedFunction> Exports,
    int? OnLoadEntryLabel,
    IReadOnlyList<BeamLambdaEntry> Lambdas
)
{
    internal IReadOnlySet<string> RegistryModuleDependencies { get; init; } = FrozenSet<string>.Empty;
    internal IReadOnlyList<Term> SourceLocations { get; init; } = Array.Empty<Term>();
    internal ConcurrentDictionary<int, object> ContinuationIdentities { get; } = new();
}
