namespace Erlang.Differential;

public static class AdditionalTypePredicateCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            AdditionalTypePredicateCaseNames.IsBoolean,
            AdditionalTypePredicateCaseSources.IsBoolean,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(AdditionalTypePredicateCaseNames.Done))
        ),
        new(
            AdditionalTypePredicateCaseNames.IsFunction,
            AdditionalTypePredicateCaseSources.IsFunction,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(AdditionalTypePredicateCaseNames.Done))
        ),
        new(
            AdditionalTypePredicateCaseNames.IsList,
            AdditionalTypePredicateCaseSources.IsList,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(AdditionalTypePredicateCaseNames.Done))
        ),
        new(
            AdditionalTypePredicateCaseNames.IsPid,
            AdditionalTypePredicateCaseSources.IsPid,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(AdditionalTypePredicateCaseNames.Done))
        ),
        new(
            AdditionalTypePredicateCaseNames.IsPort,
            AdditionalTypePredicateCaseSources.IsPort,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(AdditionalTypePredicateCaseNames.Done))
        ),
        new(
            AdditionalTypePredicateCaseNames.IsReference,
            AdditionalTypePredicateCaseSources.IsReference,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(AdditionalTypePredicateCaseNames.Done))
        )
    ];
}
