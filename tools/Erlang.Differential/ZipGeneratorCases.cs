namespace Erlang.Differential;

public static class ZipGeneratorCases
{
    private static Term Ok(Term value) => Term.Tuple(Term.A("ok"), value);

    private static Term Numbers(params long[] values) => Cons.From(values.Select(Term.I));

    private static Term Map(params (string Key, long Value)[] fields) => new MapTerm(fields.Select(field => new KeyValuePair<Term, Term>(Term.A(field.Key), Term.I(field.Value))));

    private static Term Bytes(params byte[] values) => new BitString(values);

    private static Term Error(string reason, Term value) => Term.Tuple(Term.A("error"), Term.A("error"), Term.Tuple(Term.A(reason), value));

    private static Term Bad(params Term[] remaining) => Error("bad_generators", new TupleTerm(remaining));

    private static Term Pair(long first, long second) => Term.Tuple(Term.I(first), Term.I(second));

    private static Term Triple(string key, long value, long other) => Term.Tuple(Term.A(key), Term.I(value), Term.I(other));

    private static Term BadTag => Term.Tuple(Term.A("bad"), Term.I(2));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("zip-lists", "[{X,Y} || X <- [1,2] && Y <- [3,4]]", Ok(Term.List(Pair(1,3),Pair(2,4)))),
        new("zip-three", "[X+Y+Z || X <- [1,2] && Y <- [10,20] && Z <- [100,200]]", Ok(Numbers(111,222))),
        new(
            "zip-before",
            "[{N,X,Y} || N <- [1,2], X <- [N] && Y <- [10]]",
            Ok(Term.List(Term.Tuple(Term.I(1),Term.I(1),Term.I(10)),Term.Tuple(Term.I(2),Term.I(2),Term.I(10))))
        ),
        new("zip-after", "[X+Y+Z || X <- [1,2] && Y <- [10,20], Z <- [100,200]]", Ok(Numbers(
            111,
            211,
            122,
            222
        ))),
        new("zip-two-groups", "[X+Y+Z+W || X <- [1] && Y <- [2], Z <- [3,4] && W <- [5,6]]", Ok(Numbers(11,13))),
        new("zip-filter", "[X+Y || X <- [1,2,3] && Y <- [10,20,30],X>1]", Ok(Numbers(22,33))),
        new(
            "zip-relaxed-pattern",
            "[{X,Y} || {ok,X} <- [{ok,1},{bad,2},{ok,3}] && Y <- [10,20,30]]",
            Ok(Term.List(Pair(1,10),Pair(3,30)))
        ),
        new("zip-shared-variable", "[X || X <- [1,2,3] && X <- [1,0,3]]", Ok(Numbers(1,3))),
        new("zip-strict-conflict", "[X || X <- [1,2,3] && X <:- [1,0,3]]", Bad(Numbers(2,3),Numbers(0,3))),
        new("zip-strict-first", "[X || X <:- [1,2] && X <- [1,3]]", Bad(Numbers(2),Numbers(3))),
        new("zip-later-strict-name", "[X || X <- [1] && X <- [2] && X <:- [3]]", Bad(Numbers(1),Numbers(2),Numbers(3))),
        new("zip-skip-strict-tag", "[X || {ok,X} <- [{bad,2}] && X <:- [1]]", Ok(Nil.Value)),
        new("zip-skip-unrelated-strict", "[Y || {ok,X} <- [{bad,2}] && X <- [1] && Y <:- [a]]", Ok(Nil.Value)),
        new("zip-alias", "[X || Whole={ok,X} <- [{ok,1},{ok,2}] && Whole <- [{ok,1},{ok,2}]]", Ok(Numbers(1,2))),
        new("zip-length-left", "[X+Y || X <- [1] && Y <- [10,20]]", Bad(Nil.Value,Numbers(20))),
        new("zip-length-right", "[X+Y || X <- [1,2] && Y <- [10]]", Bad(Numbers(2),Nil.Value)),
        new("zip-empty", "[error(unreachable) || X <- [] && Y <- []]", Ok(Nil.Value)),
        new("zip-improper-tail", "[X+Y || X <- [1|tail] && Y <- [10,20]]", Bad(Term.A("tail"),Numbers(20))),
        new("zip-invalid-list", "[X || X <- wrong && Y <- [1]]", Bad(Term.A("wrong"),Numbers(1))),
        new(
            "zip-source-error-before-validation",
            "[X || X <- wrong && Y <- error(source_failed)]",
            Term.Tuple(Term.A("error"),Term.A("error"),Term.A("source_failed"))
        ),
        new(
            "zip-shadow-private",
            "begin X=99,L=[{X,Y} || X <- [1] && Y <- [2]],{L,X} end",
            Ok(Term.Tuple(Term.List(Pair(1,2)),Term.I(99)))
        ),
        new("zip-source-binding-dependency", "[X+Y || X <- begin T=[1],T end && Y <- T]", Error("unbound",Term.A("T"))),
        new("zip-source-private", "[X+Y || X <- begin T=[1],T end && Y <- [2]]", Ok(Numbers(3))),
        new(
            "zip-effect-order",
            "begin put(log,[]),[begin put(log,[body|get(log)]),X+Y end || X <- begin put(log,[first|get(log)]),[1] end && Y <- begin put(log,[second|get(log)]),[2] end],get(log) end",
            Ok(Term.List(Term.A("body"),Term.A("second"),Term.A("first")))
        ),
        new("zip-binary-list", "[X+Y || <<X>> <= <<1,2>> && Y <- [10,20]]", Ok(Numbers(11,22))),
        new("zip-binary-binary", "[X+Y || <<X>> <= <<1,2>> && <<Y>> <= <<3,4>>]", Ok(Numbers(4,6))),
        new("zip-binary-shared-variable", "[X || <<X>> <= <<1,2,3>> && <<X>> <= <<1,0,3>>]", Ok(Numbers(1,3))),
        new("zip-unaligned", "[X || <<X:3>> <= <<1:3,2:3,3:3>> && _ <- [a,b,c]]", Ok(Numbers(1,2,3))),
        new("zip-binary-partial", "[X+Y || <<X>> <= <<1,2:3>> && Y <- [10]]", Ok(Numbers(11))),
        new("zip-binary-strict-partial", "[X+Y || <<X>> <:= <<1,2:3>> && Y <- [10]]", Bad(Bytes(),Nil.Value)),
        new("zip-strict-partial-list-first", "[X+Y || Y <- [10] && <<X>> <:= <<1,2:3>>]", Ok(Numbers(11))),
        new("zip-binary-long-list-empty", "[X+Y || <<X>> <= <<1,2>> && Y <- [10]]", Bad(Bytes(2),Nil.Value)),
        new("zip-binary-short", "[X+Y || <<X>> <= <<1>> && Y <- [10,20]]", Bad(Bytes(),Numbers(20))),
        new("zip-binary-mismatch", "[X+Y || <<0,X>> <= <<1,10,0,20>> && Y <- [1,2]]", Ok(Numbers(22))),
        new("zip-binary-strict-mismatch", "[X+Y || <<0,X>> <:= <<1,10>> && Y <- [1]]", Bad(Bytes(1,10),Numbers(1))),
        new("zip-binary-dynamic", "[X+Y || <<N:4,X:N>> <= <<3:4,5:3,2:4,1:2>> && Y <- [10,20]]", Ok(Numbers(15,21))),
        new("zip-binary-utf", "[X+Y || <<X/utf8>> <= <<65,226,130,172>> && Y <- [1,2]]", Ok(Numbers(66,8366))),
        new("zip-binary-signed", "[X+Y || <<X:16/signed-little>> <= <<255,255,0,128>> && Y <- [1,2]]", Ok(Numbers(0,-32766))),
        new("zip-binary-template", "<< <<(X+Y):4>> || X <- [1,2] && Y <- [3,4] >>", Ok(Bytes(70))),
        new("zip-binary-incoming-size", "begin N=8,[X || <<X:N>> <= <<1>> && Y <- [1]] end", Ok(Nil.Value)),
        new("zip-zero-binary-finite-list", "[X+Y || <<X:0>> <= <<>> && Y <- [1,2]]", Bad(Bytes(),Numbers(1,2))),
        new("zip-map-list", "[{K,V,Y} || K := V <- #{a=>1,b=>2} && Y <- [10,20]]", Ok(Term.List(Triple("a",1,10),Triple("b",2,20)))),
        new("zip-map-map", "[V+W || K := V <- #{a=>1,b=>2} && K := W <- #{a=>3,b=>4}]", Ok(Numbers(4,6))),
        new("zip-map-binary", "[V+X || _ := V <- #{a=>1,b=>2} && <<X>> <= <<10,20>>]", Ok(Numbers(11,22))),
        new("zip-map-iterator", "[V+Y || _ := V <- maps:iterator(#{a=>1,b=>2},reversed) && Y <- [10,20]]", Ok(Numbers(12,21))),
        new("zip-map-tuple-chain", "[V+Y || _ := V <- {a,1,{b,2,none}} && Y <- [10,20]]", Ok(Numbers(11,22))),
        new("zip-map-none", "[V || _ := V <- none && Y <- []]", Ok(Nil.Value)),
        new("zip-map-short", "[V+Y || _ := V <- #{a=>1} && Y <- [10,20]]", Bad(Map(),Numbers(20))),
        new("zip-map-long", "[V+Y || _ := V <- #{a=>1,b=>2} && Y <- [10]]", Bad(Map(("b",2)),Nil.Value)),
        new("zip-map-strict", "[V+Y || a := V <:- #{a=>1,b=>2} && Y <- [10,20]]", Bad(Map(("b",2)),Numbers(20))),
        new(
            "zip-invalid-map-before-source-error",
            "[V || _ := V <- wrong && Y <- error(source_failed)]",
            Error("bad_generator",Term.A("wrong"))
        ),
        new(
            "zip-map-template",
            "#{X=>Y || X <- [1,2] && Y <- [10,20]}",
            Ok(new MapTerm([new(Term.I(1),Term.I(10)),new(Term.I(2),Term.I(20))]))
        ),
        new("zip-incoming-map-key", "begin Key=a,[V || #{Key:=V} <- [#{a=>1}] && X <- [1]] end", Ok(Nil.Value)),
        new("zip-shadow-map-key", "begin Key=a,[V || Key <- [b] && #{Key:=V} <- [#{a=>1,b=>2}]] end", Ok(Numbers(2)))
    ];

    private static Term CompiledExpected(OperatorExpressionCase fixture) => fixture.Name switch
    {
        "zip-source-binding-dependency" => Ok(Numbers(2)),
        "zip-skip-strict-tag" => Bad(Term.List(BadTag), Numbers(1)),
        "zip-binary-strict-partial" => Bad(new BitString([64], 3), Nil.Value),
        "zip-strict-partial-list-first" => Bad(Nil.Value, new BitString([64], 3)),
        "zip-binary-long-list-empty" => Ok(Numbers(11)),
        "zip-zero-binary-finite-list" => Ok(Numbers(1, 2)),
        "zip-binary-shared-variable" => Ok(Numbers(1)),
        "zip-binary-incoming-size" or "zip-incoming-map-key" or "zip-shadow-map-key" => Ok(Numbers(1)),
        "zip-map-short" => Bad(Term.A("none"), Numbers(20)),
        "zip-map-long" => Bad(Term.Tuple(Term.A("b"), Term.I(2), Term.A("none")), Nil.Value),
        "zip-map-strict" => Bad(Term.Tuple(Term.A("b"), Term.I(2), Term.A("none")), Numbers(20)),
        _ => fixture.Expected
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_zip_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        CompiledExpected(fixture)
    )).ToArray();
}
