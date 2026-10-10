namespace Erlang.Differential;

public static class BitEvaluationCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Error(Term reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), reason);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("bits-float-conversion-before-huge-size", "<<(1 bsl 2000):64/float,0:(1 bsl 100)>>", Error(Term.A("badarg"))),
        new("bits-utf-before-huge-size", "<<55296/utf8,0:(1 bsl 100)>>", Error(Term.A("badarg"))),
        new("bits-negative-size-before-huge-size", "<<1:(-1),0:(1 bsl 100)>>", Error(Term.A("badarg"))),
        new("bits-float-conversion-after-evaluation", "<<(1 bsl 2000):64/float,(error(later)):8>>", Error(Term.A("later"))),
        new("bits-invalid-value-after-size-evaluation", "<<bad:(error(size_first))>>", Error(Term.A("size_first"))),
        new(
            "bits-invalid-value-before-next-size-effect",
            "begin put(mark,none),R=catch <<bad:8,1:(begin put(mark,size_done),8 end)>>,{'EXIT',{Reason,_}}=R,{Reason,get(mark)} end",
            Success(Term.Tuple(Term.A("badarg"),Term.A("size_done")))
        ),
        new(
            "bits-value-size-export",
            "begin B = <<(X=1):(S=4),2:4>>,{B,X,S} end",
            Success(Term.Tuple(new BitString([18]),Term.I(1),Term.I(4)))
        ),
        new("bits-equal-binding", "begin B = <<(X=1),(X=1)>>,{B,X} end", Success(Term.Tuple(new BitString([1,1]),Term.I(1)))),
        new("bits-conflicting-binding", "<<(X=1),(X=2)>>", Error(Term.Tuple(Term.A("badmatch"),Term.I(2)))),
        new("bits-string-size-export", "begin B = <<\"ab\":(S=4)>>,{B,S} end", Success(Term.Tuple(new BitString([18]),Term.I(4)))),
        new(
            "bits-empty-string-valid-size",
            "begin B = <<\"\":(S=4),3:4>>,{B,S} end",
            Success(Term.Tuple(new BitString([48],4),Term.I(4)))
        ),
        new("bits-empty-string-error-after-evaluation", "<<\"\":(-1),(error(later)):8>>", Error(Term.A("later")))
    ];
}
