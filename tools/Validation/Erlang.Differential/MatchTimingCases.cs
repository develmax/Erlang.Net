namespace Erlang.Differential;

public static class MatchTimingCases
{
    private static Term Error(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), Term.Tuple(Term.A("badmatch"), value));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("match-tuple-sibling-structure", "{{X,Y}={1,2},{X,Y}={3,4}}", Error(Term.I(1))),
        new("match-list-sibling-structure", "[{X,Y}={1,2},{X,Y}={3,4}]", Error(Term.I(3))),
        new("match-prebound-whole-value", "begin X=1,{X,Y}={2,3} end", Error(Term.Tuple(Term.I(2),Term.I(3))))
    ];
}
