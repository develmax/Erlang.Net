namespace Erlang.Differential;

public static class MapBindingCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Error(Term reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), reason);

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new(
            "map-fields-export",
            "begin M=#{(K=a)=>(V=42)},{M,K,V} end",
            Success(Term.Tuple(new MapTerm([new(Term.A("a"),Term.I(42))]),Term.A("a"),Term.I(42)))
        ),
        new(
            "map-base-export",
            "begin M=(B=#{a=>1})#{b=>2},{M,B} end",
            Success(Term.Tuple(new MapTerm([new(Term.A("a"),Term.I(1)),new(Term.A("b"),Term.I(2))]),new MapTerm([new(Term.A("a"),Term.I(1))])))
        ),
        new("map-base-field-conflict", "(X=#{})#{a=>(X=1)}", Error(Term.Tuple(Term.A("badmatch"),new MapTerm([])))),
        new("map-base-field-equal", "begin (X=#{})#{a=>(X=#{})},X end", Success(new MapTerm([]))),
        new("map-field-conflict", "#{a=>(X=1),b=>(X=2)}", Error(Term.Tuple(Term.A("badmatch"),Term.I(2)))),
        new("map-exact-after-assoc", "#{}#{a=>1,a:=2}", Success(new MapTerm([new(Term.A("a"),Term.I(2))]))),
        new("map-fields-before-badmap", "(notmap)#{a=>error(field)}", Error(Term.A("field"))),
        new("map-badkey-before-base-merge", "(X=#{})#{a:=(X=1)}", Error(Term.Tuple(Term.A("badkey"),Term.A("a")))),
        new(
            "map-fields-effect-order",
            "begin put(mark,0),M=#{put(mark,1)=>put(mark,2),put(mark,3)=>put(mark,4)},{M,get(mark)} end",
            Success(Term.Tuple(new MapTerm([new(Term.I(0),Term.I(1)),new(Term.I(2),Term.I(3))]),Term.I(4)))
        ),
        new(
            "map-signed-zero-and-numeric-keys",
            "#{0.0=>first,-0.0=>last,1=>integer,1.0=>float}",
            Success(new MapTerm([new(new FloatTerm(0),Term.A("first")),new(new FloatTerm(-0d),Term.A("last")),new(Term.I(1),Term.A("integer")),new(new FloatTerm(1),Term.A("float"))]))
        )
    ];
}
