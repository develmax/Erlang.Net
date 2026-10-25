namespace Erlang.Differential;

public static class TypePredicateCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            TypePredicateCaseNames.IsAtom,
            TypePredicateCaseSources.IsAtom,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TypePredicateCaseNames.Done))
        ),
        new(
            TypePredicateCaseNames.IsInteger,
            TypePredicateCaseSources.IsInteger,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TypePredicateCaseNames.Done))
        ),
        new(
            TypePredicateCaseNames.IsFloat,
            TypePredicateCaseSources.IsFloat,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TypePredicateCaseNames.Done))
        ),
        new(
            TypePredicateCaseNames.IsNumber,
            TypePredicateCaseSources.IsNumber,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TypePredicateCaseNames.Done))
        ),
        new(
            TypePredicateCaseNames.IsTuple,
            TypePredicateCaseSources.IsTuple,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(TypePredicateCaseNames.Done))
        )
    ];
}
