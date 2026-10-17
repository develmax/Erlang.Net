namespace Erlang.Differential;

public static class SpawnPriorityCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            SpawnPriorityCaseNames.FirstObservation,
            SpawnPriorityCaseSources.FirstObservation,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SpawnPriorityCaseNames.Done))
        ),
        new(
            SpawnPriorityCaseNames.FlagOldAndDefault,
            SpawnPriorityCaseSources.FlagOldAndDefault,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SpawnPriorityCaseNames.Done))
        ),
        new(
            SpawnPriorityCaseNames.DuplicateInvalid,
            SpawnPriorityCaseSources.DuplicateInvalid,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SpawnPriorityCaseNames.Done))
        )
    ];
}
