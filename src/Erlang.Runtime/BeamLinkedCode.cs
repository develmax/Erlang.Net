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
);
