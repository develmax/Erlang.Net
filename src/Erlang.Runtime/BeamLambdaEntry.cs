namespace Erlang;

internal sealed record BeamLambdaEntry(
    int FunctionAtomIndex,
    int TotalArity,
    int Label,
    int StoredIndex,
    int FreeVariables,
    int OldUniqueness
);
