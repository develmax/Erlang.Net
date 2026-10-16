namespace Erlang.Differential;

public static class NumericBoundaryCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            NumericBoundaryCaseNames.AbsBitBoundaries,
            NumericBoundaryCaseSources.AbsBitBoundaries,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.FloatPreservesBits,
            NumericBoundaryCaseSources.FloatPreservesBits,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.FloatNearestEven,
            NumericBoundaryCaseSources.FloatNearestEven,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.FloatOverflowThreshold,
            NumericBoundaryCaseSources.FloatOverflowThreshold,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.RoundHalfNeighbours,
            NumericBoundaryCaseSources.RoundHalfNeighbours,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.SubnormalDirectedRounding,
            NumericBoundaryCaseSources.SubnormalDirectedRounding,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.LargestFloatIntegers,
            NumericBoundaryCaseSources.LargestFloatIntegers,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.HugeIntegerIdentity,
            NumericBoundaryCaseSources.HugeIntegerIdentity,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.InvalidTypePartitions,
            NumericBoundaryCaseSources.InvalidTypePartitions,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.MinMaxTieIdentity,
            NumericBoundaryCaseSources.MinMaxTieIdentity,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.MinMaxExactMixedComparison,
            NumericBoundaryCaseSources.MinMaxExactMixedComparison,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        ),
        new(
            NumericBoundaryCaseNames.GuardAndMatchSpecRoutes,
            NumericBoundaryCaseSources.GuardAndMatchSpecRoutes,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(NumericBoundaryCaseNames.Done))
        )
    ];
}
