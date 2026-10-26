using System.Collections.Frozen;
using System.Collections.Concurrent;
using System.Numerics;

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
    private readonly ConcurrentDictionary<BigInteger, Integer> integerConstants = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<NativeRecordDefinition>> nativeRecords = new(StringComparer.Ordinal);

    internal IReadOnlyList<NativeRecordDefinition> NativeRecords
        => nativeRecords.GetOrAdd(BeamPreparationNames.RecordsChunk, static (_, code) => BeamRecordMetadata.Read(code), this);

    internal IReadOnlySet<string> RegistryModuleDependencies { get; init; } = FrozenSet<string>.Empty;
    internal IReadOnlyList<Term> SourceLocations { get; init; } = Array.Empty<Term>();
    internal ConcurrentDictionary<int, object> ContinuationIdentities { get; } = new();

    internal Integer IntegerConstant(BigInteger value) => integerConstants.GetOrAdd(value, static number => new Integer(number));
}
