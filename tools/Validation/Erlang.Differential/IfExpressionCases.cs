namespace Erlang.Differential;

public static class IfExpressionCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Error(Term reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), reason);

    public static IReadOnlyList<IfExpressionCase> All { get; } = [
        new("ordered-first", "if true -> first; true -> second end", Success(Term.A("first"))),
        new("false-fallback", "if false -> no; true -> yes end", Success(Term.A("yes"))),
        new("nonboolean-fallback", "if atom -> no; true -> yes end", Success(Term.A("yes"))),
        new("arithmetic-guard-failure", "if 1 div 0 =:= 0 -> no; true -> yes end", Success(Term.A("yes"))),
        new("bif-alternative-recovery", "if hd(atom) =:= 1; is_atom(atom) -> yes end", Success(Term.A("yes"))),
        new("comma-conjunction-failure", "if is_integer(1), 1 > 2 -> no; true -> yes end", Success(Term.A("yes"))),
        new("comma-conjunction-success", "if is_integer(1), 1 < 2 -> yes end", Success(Term.A("yes"))),
        new("semicolon-alternatives", "if false; true -> yes end", Success(Term.A("yes"))),
        new("short-circuit", "if false andalso hd(atom) -> no; true orelse hd(atom) -> yes end", Success(Term.A("yes"))),
        new("no-match", "if false -> no end", Error(Term.A("if_clause"))),
        new("all-guards-fail", "if hd(atom) -> no; 1 div 0 =:= 0 -> no end", Error(Term.A("if_clause"))),
        new("body-error-propagates", "if true -> error(selected); true -> no end", Error(Term.A("selected"))),
        new(
            "selected-side-effect",
            "case ok of ok -> put(counter,0),if false -> put(counter,1); true -> put(counter,2) end,get(counter) end",
            Success(Term.I(2))
        ),
        new("unselected-body-not-run", "if false -> error(unselected); true -> 42 end", Success(Term.I(42))),
        new("common-binding-first", "case ok of ok -> if true -> X=1; true -> X=2 end,X end", Success(Term.I(1))),
        new("common-binding-second", "case ok of ok -> if false -> X=1; true -> X=2 end,X end", Success(Term.I(2))),
        new("prebound-match", "case ok of ok -> X=42,if true -> X=42 end,X end", Success(Term.I(42))),
        new("prebound-badmatch", "case ok of ok -> X=1,if true -> X=2 end end", Error(Term.Tuple(Term.A("badmatch"),Term.I(2)))),
        new("nested-if", "if true -> if false -> no; true -> 42 end end", Success(Term.I(42))),
        new("closure-capture", "case ok of ok -> X=40,F=fun(Y)->if Y > 0 -> X+Y; true -> X end end,F(2) end", Success(Term.I(42))),
        new("nonboolean-last-conjunct", "if true, atom -> no; true -> yes end", Success(Term.A("yes"))),
        new(
            "map-bit-guard",
            "if map_get(<<1:3>>,#{<<1:3>>=>42}) =:= 42 -> <<\"😀\"/utf8>> end",
            Success(new BitString([240,159,152,128]))
        ),
        new("case-if-nesting", "case 1 of X -> if X =:= 1 -> case ok of ok -> 42 end end end", Success(Term.I(42))),
        new("quoted-if-atom", "if 'if' =:= 'if' -> 'if' end", Success(Term.A("if")))
    ];
}
