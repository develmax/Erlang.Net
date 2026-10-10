namespace Erlang.Differential;

public static class BitEmptyStringCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Error(string reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), Term.A(reason));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("empty-string-dynamic-negative-later-error", "begin N = -1, <<\"\":N,(error(later)):8>> end", Error("later")),
        new("empty-string-dynamic-atom-later-error", "begin N = bad, <<\"\":N,(error(later)):8>> end", Error("later")),
        new(
            "empty-string-dynamic-negative-later-effect",
            "begin put(mark,none),N = -1,R=catch <<\"\":N,(begin put(mark,after_size),1 end):8>>,{'EXIT',{Reason,_}}=R,{Reason,get(mark)} end",
            Success(Term.Tuple(Term.A("badarg"),Term.A("after_size")))
        ),
        new(
            "empty-string-size-binding-export",
            "begin B = <<\"\":(N=4),3:4>>,{B,N} end",
            Success(Term.Tuple(new BitString([48],4),Term.I(4)))
        ),
        new("empty-string-float-unused-width", "<<\"\":7/float>>", Error("badarg")),
        new("empty-string-unused-huge-size", "begin N = 1 bsl 100, <<\"\":N>> end", Error("system_limit"))
    ];
}
